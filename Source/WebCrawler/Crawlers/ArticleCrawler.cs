using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Threading.Tasks.Dataflow;
using WebCrawler.Analyzers;
using WebCrawler.Common;
using WebCrawler.DataLayer;
using WebCrawler.Models;

namespace WebCrawler.Crawlers
{
    public class ArticleCrawler : ICrawler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDataLayer _dataLayer;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly CrawlSettings _crawlSettings;

        public event EventHandler<WebsiteCrawlingEventArgs> WebsiteCrawling;
        public event EventHandler<CrawlCompletedEventArgs> Completed;
        public event EventHandler<CrawlMessagingEventArgs> Messaging;
        public event EventHandler<CrawlProgressChangedEventArgs> StatusChanged;

        public ArticleCrawler(IServiceProvider serviceProvider, IDataLayer dataLayer, IHttpClientFactory clientFactory, CrawlSettings crawlSettings, ILogger<ArticleCrawler> logger)
        {
            _serviceProvider = serviceProvider;
            _dataLayer = dataLayer;
            _logger = logger;
            _httpClient = clientFactory.CreateClient(Constants.HTTP_CLIENT_NAME_DEFAULT);
            _crawlSettings = crawlSettings;
        }

        public async Task ExecuteAsync(bool continuePrevious = false)
        {
            Crawl crawl = null;
            try
            {
                var crawls = (await _dataLayer.GetCrawlsAsync()).Items;

                crawl = crawls.FirstOrDefault();
                if (continuePrevious && (crawl?.Status == CrawlStatus.Failed || crawl?.Status == CrawlStatus.Cancelled))
                {
                    crawl = await _dataLayer.ContinueCrawlAsync(crawl.Id);
                }
                else
                {
                    crawl = await _dataLayer.QueueCrawlAsync();
                }

                HandleMessaging($"Started {(continuePrevious ? "incremental" : "full")} crawling");

                ActionBlock<CrawlLog> workerBlock = new ActionBlock<CrawlLog>(async crawlLog =>
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

                    OnStatusChanged(new CrawlProgressChangedEventArgs(crawl.Success, crawl.Fail));
                }, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _crawlSettings.MaxDegreeOfParallelism
                });

                PagedResult<CrawlLog> crawlLogsQueue = null;
                do
                {
                    crawlLogsQueue = await _dataLayer.GetCrawlingQueueAsync(crawl.Id, crawlLogsQueue?.Items.Last().Id);

                    if (crawlLogsQueue.PageInfo.CurrentPage == 1)
                    {
                        OnStatusChanged(new CrawlProgressChangedEventArgs(crawl.Success, crawl.Fail, crawlLogsQueue.PageInfo.ItemCount));
                    }

                    foreach (var websiteCrawlLog in crawlLogsQueue.Items)
                    {
                        workerBlock.Post(websiteCrawlLog);

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
                HandleMessaging($"Completed {(continuePrevious ? "previous" : "new")} crawling");
            }

            OnCompleted(new CrawlCompletedEventArgs(crawl));
        }

        protected virtual void OnWebsiteCrawling(WebsiteCrawlingEventArgs e)
        {
            WebsiteCrawling?.Invoke(this, e);
        }

        protected virtual void OnCompleted(CrawlCompletedEventArgs e)
        {
            Completed?.Invoke(this, e);
        }

        protected virtual void OnMessaging(CrawlMessagingEventArgs e)
        {
            //_logger.LogInformation(e.Message);

            Messaging?.Invoke(this, e);
        }

        protected virtual void OnStatusChanged(CrawlProgressChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        #region Private Members

        private async Task CrawlWebsiteAsync(CrawlLog crawlLog)
        {
            crawlLog.Status = CrawlStatus.Crawling;
            crawlLog.Crawled = DateTime.Now;

            var articles = new List<Article>();

            CatalogItem[] catalogItems = null;
            try
            {
                var data = await HtmlHelper.GetHtmlAsync(crawlLog.Website.Home, _httpClient);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(data.Content);

                catalogItems = HtmlAnalyzer.DetectCatalogItems(htmlDoc, crawlLog.Website.ListPath, crawlLog.Website.ValidateDate);
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
                        var data = await HtmlHelper.GetHtmlAsync(item.Url, _httpClient);
                        var info = Html2Article.GetArticle(data.Content);

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

                        HandleMessaging($"Crawled article: {item.Url}", LogLevel.Trace);
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, $"Failed to get article: {item.Url}");

                        crawlLog.Fail++;
                    }

                    OnWebsiteCrawling(new WebsiteCrawlingEventArgs(crawlLog));
                }

                if (crawlLog.Success == 0 && crawlLog.Fail > 0)
                {
                    crawlLog.Status = CrawlStatus.Failed;
                    crawlLog.Notes = "Failed as nothing succeeded";
                }

                OnWebsiteCrawling(new WebsiteCrawlingEventArgs(crawlLog));
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
                    HandleMessaging($"No updates: {crawlLog.Website.Home}");
                }
                else
                {
                    HandleMessaging($"Completed website crawling: {crawlLog.Website.Home}");
                }
            }
            else if (crawlLog.Status == CrawlStatus.Failed)
            {
                HandleMessaging($"Failed to crawl website due to '{crawlLog.Notes}': {crawlLog.Website.Home}", LogLevel.Error);
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

                OnMessaging(new CrawlMessagingEventArgs(string.IsNullOrEmpty(message) ? exception.Message : message));
            }
        }

        private void HandleMessaging(string message, LogLevel level = LogLevel.Information)
        {
            switch (level)
            {
                case LogLevel.Warning:
                case LogLevel.Error:
                case LogLevel.Critical:
                    _logger.LogError(message);
                    break;
                case LogLevel.Trace:
                    _logger.LogTrace(message);
                    break;
                default:
                    _logger.LogInformation(message);
                    break;
            }

            OnMessaging(new CrawlMessagingEventArgs(message));
        }

        #endregion
    }
}
