using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using WebCrawler.Core;
using WebCrawler.Models;
using WebCrawler.Persisters;

namespace WebCrawler.Crawlers
{
    public class ArticleCrawler : ICrawler
    {
        private readonly static int BATCH_SIZE = 100;

        private readonly CrawlingSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly IPersister _persister;
        private readonly ILogger _logger;

        private readonly ActionBlock<WebsiteParser> _workerBlock;

        public ArticleCrawler(CrawlingSettings settings, IPersister persister, IHttpClientFactory clientFactory, ILogger logger)
        {
            _settings = settings;
            _persister = persister;
            _httpClient = clientFactory.CreateClient(Constants.HTTP_CLIENT_NAME_DEFAULT);
            _logger = logger;

            _workerBlock = new ActionBlock<WebsiteParser>(
                CrawlWebsiteAsync,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _settings.MaxDegreeOfParallelism
                });
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Loading websites data");

            var configs = await _persister.GetConfigsAsync();

            foreach (var config in configs)
            {
                _workerBlock.Post(config);
            }

            _workerBlock.Complete();
            _workerBlock.Completion.Wait();

            _logger.LogInformation("All completed!");
        }

        private int _total = 0;
        private int _accessibles = 0;
        private int _correctListPath = 0;

        private async Task CrawlWebsiteAsync(WebsiteParser webConfig)
        {
            lock (this)
            {
                _total++;
            }

            if (string.IsNullOrEmpty(webConfig.Website.Previous))
            {
                _logger.LogInformation("Crawling {0} feed catalogs fully", webConfig.Website.Name);
            }
            else
            {
                _logger.LogInformation("Crawling {0} feed catalogs from last article: {1}", webConfig.Website.Name, webConfig.Website.Previous);
            }

            var listPath = "//" + webConfig.ListPath + "/@href";
            listPath = listPath.Replace(">", "/");
            listPath = Regex.Replace(listPath, @"\.([^./> ]+)", "[@class='$1']");
            listPath = Regex.Replace(listPath, @"\#([^./> ]+)", "[@id='$1']");

            var articles = new List<Article>();

            var page = -1;
            string feedUrl = null;
            bool exceedPrevious = false;
            string content;
            do
            {
                try
                {
                    feedUrl = webConfig.Website.Home;// string.IsNullOrEmpty(config.Website.Url) ? config.Website.Home : config.Website.Url.Replace("%PAGE", page.ToString());

                    _logger.LogDebug("Crawling {0} feed catalogs: {1}", webConfig.Website.Name, feedUrl);

                    content = await _httpClient.GetStringAsync(feedUrl);

                    lock (this)
                    {
                        _accessibles++;
                    }

                    var feedHtmlDoc = new HtmlDocument();
                    feedHtmlDoc.LoadHtml(content);

                    var feedHtmlNav = feedHtmlDoc.CreateNavigator();

                    var feedItemLinkIterator = feedHtmlNav.Select(listPath);

                    // exit as no item detected
                    if (feedItemLinkIterator.Count == 0)
                    {
                        break;
                    }

                    if (feedItemLinkIterator.Count > 0)
                    {
                        lock (this)
                        {
                            _correctListPath++;
                        }
                    }

                    continue;

                    while (feedItemLinkIterator.MoveNext())
                    {
                        var articleLink = feedItemLinkIterator.Current.Value?.Trim();
                        if (!string.IsNullOrEmpty(articleLink))
                        {
                            articleLink = Utilities.ResolveResourceUrl(articleLink, feedUrl);
                            if (articleLink.Equals(webConfig.Website.Previous, StringComparison.CurrentCultureIgnoreCase))
                            {
                                exceedPrevious = true;
                                break;
                            }

                            try
                            {
                                content = await _httpClient.GetStringAsync(articleLink);

                                var tempArticle = StanSoft.Html2Article.GetArticle(content);

                                articles.Add(new Article
                                {
                                    Url = articleLink,
                                    WebsiteId = webConfig.WebsiteId,
                                    Title = tempArticle.Title,
                                    Content = tempArticle.Content,
                                    ContentHtml = tempArticle.ContentWithTags,
                                    Published = tempArticle.PublishDate
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to retrieve {0} feed article: {1}", webConfig.Website.Name, articleLink);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve {0} feed catalogs: {1}", webConfig.Website.Name, feedUrl);

                    _logger.LogInformation($"Accessibles: {_accessibles}, CorrectListPath: {_correctListPath}, Total: {_total}");

                    // exit without saving
                    return;
                }

                //if (!string.IsNullOrEmpty(config.Website.Url))
                //{
                //    page++;
                //}
            } while (page >= 0
                && !exceedPrevious
            /*&& (_settings.FeedMaxPagesLimit == -1 || page < _settings.FeedPageIndexStart + _settings.FeedMaxPagesLimit)*/
            );

            _logger.LogInformation("Crawled {0} feed catalogs: {1} articles", webConfig.Website.Name, articles.Count);


            await _persister.AddAsync(articles);

            _logger.LogInformation("Persisted {0} feed catalogs: {1} articles", webConfig.Website.Name, articles.Count);

            _logger.LogInformation($"Accessibles: {_accessibles}, CorrectListPath: {_correctListPath}, Total: {_total}");
        }
    }
}
