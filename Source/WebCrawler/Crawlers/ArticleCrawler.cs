using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Threading.Tasks.Dataflow;
using WebCrawler.Analyzers;
using WebCrawler.Common;
using WebCrawler.DataLayer;
using WebCrawler.DTO;
using WebCrawler.Models;
using WebCrawler.Queue;

namespace WebCrawler.Crawlers
{
    public class ArticleCrawler : ICrawler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDataLayer _dataLayer;
        private readonly IProxyDispatcher _proxyDispatcher;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly CrawlSettings _crawlSettings;

        public ArticleCrawler(IServiceProvider serviceProvider, IDataLayer dataLayer, IHttpClientFactory clientFactory, IProxyDispatcher proxyDispatcher, CrawlSettings crawlSettings, ILogger<ArticleCrawler> logger)
        {
            _serviceProvider = serviceProvider;
            _dataLayer = dataLayer;
            _proxyDispatcher = proxyDispatcher;
            _logger = logger;
            _httpClient = clientFactory.CreateClient(Constants.HTTP_CLIENT_NAME_DEFAULT);
            _crawlSettings = crawlSettings;
        }

        public async Task ExecuteAsync(bool continuePrevious = false)
        {
            try
            {
                var crawls = (await _dataLayer.GetCrawlsAsync()).Items;

                CrawlDTO? crawl = crawls.FirstOrDefault();
                if (continuePrevious && (crawl?.Status == CrawlStatus.Failed || crawl?.Status == CrawlStatus.Cancelled))
                {
                    crawl = await _dataLayer.ContinueCrawlAsync(crawl.Id);
                }
                else
                {
                    crawl = await _dataLayer.QueueCrawlAsync();
                }

                _logger.LogInformation($"Started {(continuePrevious ? "incremental" : "full")} crawling");

                int total = 0;
                ActionBlock<CrawlLogDTO> workerBlock = new ActionBlock<CrawlLogDTO>(async crawlLog =>
                {
                    await CrawlWebsiteAsync(crawlLog);

                    lock (this)
                    {
                        if (crawlLog.Status == CrawlStatus.Completed)
                        {
                            crawl.Success++;
                        }
                        else
                        {
                            crawl.Fail++;
                        }
                    }

                    _logger.LogInformation($"Success: {crawl.Success} Fail: {crawl.Fail} Total: {total}");
                }, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _crawlSettings.MaxDegreeOfParallelism
                });

                PagedResult<CrawlLogDTO> crawlLogsQueue = null;
                do
                {
                    crawlLogsQueue = await _dataLayer.GetCrawlingQueueAsync(crawl.Id, crawlLogsQueue?.Items.Last().Id);

                    if (total == 0)
                    {
                        total = crawlLogsQueue.PageInfo.ItemCount;

                        _logger.LogInformation($"Success: {crawl.Success} Fail: {crawl.Fail} Total: {total}");
                    }

                    foreach (var website in crawlLogsQueue.Items)
                    {
                        workerBlock.Post(website);

                        // accept queue items in the amount of batch size x 3
                        while (workerBlock.InputCount >= _crawlSettings.MaxDegreeOfParallelism * 2)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                } while (crawlLogsQueue.PageInfo.PageCount > 1);

                workerBlock.Complete();
                workerBlock.Completion.Wait();

                crawl.Status = CrawlStatus.Completed;
                crawl.Completed = DateTime.Now;

                await _dataLayer.SaveAsync(crawl);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                _logger.LogInformation("Completed {0} crawling", continuePrevious ? "previous" : "new");
            }
        }

        #region Private Members

        private async Task CrawlWebsiteAsync(CrawlLogDTO crawlLog)
        {
            crawlLog.Status = CrawlStatus.Crawling;
            crawlLog.Crawled = DateTime.Now;

            var articles = new List<Article>();

            CatalogItem[] catalogItems = null;
            try
            {
                var data = await HtmlHelper.GetPageDataAsync(_httpClient, _proxyDispatcher, crawlLog.Website.Home, crawlLog.Website.CatalogRule);

                catalogItems = HtmlAnalyzer.DetectCatalogItems(data.Content, crawlLog.Website.CatalogRule, crawlLog.Website.ValidateDate);
                if (catalogItems.Length == 0)
                {
                    throw new AppException("Failed to locate catalog items");
                }

                if (catalogItems.All(o => o.HasDate))
                {
                    // sort by published, as some website might have highlights always shown on the top
                    catalogItems = catalogItems
                        .OrderByDescending(o => o.Published)
                        .ToArray();
                }

                // take the first x records only as some list might contains thousands of records
                catalogItems = catalogItems
                    .Take(Constants.MAX_RECORDS)
                    .ToArray();
            }
            catch (Exception ex)
            {
                crawlLog.Status = CrawlStatus.Failed;
                crawlLog.Notes = ex.Message;

                HandleException(ex, crawlLog.Website.Home);
            }

            if (crawlLog.Status == CrawlStatus.Crawling)
            {
                foreach (var item in catalogItems)
                {
                    if (item.Url.Equals(crawlLog.LastHandled, StringComparison.CurrentCultureIgnoreCase))
                    {
                        break;
                    }

                    try
                    {
                        var data = await HtmlHelper.GetPageDataAsync(_httpClient, _proxyDispatcher, item.Url, crawlLog.Website.ArticleRule);
                        var info = HtmlAnalyzer.ParseArticle(data.Content, crawlLog.Website.ArticleRule);

                        articles.Add(new Article
                        {
                            Url = item.Url,
                            ActualUrl = data.IsRedirected ? data.ActualUrl : null,
                            Title = HtmlHelper.NormalizeText(info.Title),
                            Published = info.PublishDate ?? item.Published, // use date from article details page first
                            Content = HtmlHelper.NormalizeText(info.Content),
                            ContentHtml = HtmlHelper.NormalizeHtml(info.ContentWithTags, true),
                            WebsiteId = crawlLog.WebsiteId,
                            Timestamp = DateTime.Now
                        });

                        crawlLog.Success++;

                        _logger.LogTrace("Crawled article: {0}", item.Url);
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, $"Failed to get article: {item.Url}");

                        crawlLog.Fail++;
                    }
                }

                if (crawlLog.Success == 0 && crawlLog.Fail > 0)
                {
                    crawlLog.Status = CrawlStatus.Failed;
                    crawlLog.Notes = "Failed as nothing succeeded";
                }
            }

            try
            {
                var lastHandled = crawlLog.Status != CrawlStatus.Failed ? catalogItems[0].Url : null;

                using (var dataLayer = _serviceProvider.GetRequiredService<IDataLayer>())
                {
                    await dataLayer.SaveAsync(articles, crawlLog, lastHandled);
                }
            }
            catch (Exception ex)
            {
                crawlLog.Status = CrawlStatus.Failed;
                crawlLog.Notes = $"Failed to save data: {(ex.InnerException ?? ex).Message}";

                HandleException(ex, crawlLog.Website.Home);
            }

            if (crawlLog.Status == CrawlStatus.Completed)
            {
                if (articles.Count == 0)
                {
                    _logger.LogInformation("No updates: {0}", crawlLog.Website.Home);
                }
                else
                {
                    _logger.LogInformation("Completed website crawling: {0}", crawlLog.Website.Home);
                }
            }
            else if (crawlLog.Status == CrawlStatus.Failed)
            {
                _logger.LogError("Failed to crawl website due to '{0}': {1}", crawlLog.Notes, crawlLog.Website.Home);
            }
        }

        private void HandleException(Exception exception, string message = null)
        {
            if (!(exception is HttpRequestException
                || exception is SocketException
                || exception is TaskCanceledException
                || exception is AppException))
            {
                _logger.LogError(exception, message);
            }
        }

        #endregion
    }
}
