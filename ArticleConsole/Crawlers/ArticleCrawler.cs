using ArticleConsole.Common;
using ArticleConsole.Models;
using ArticleConsole.Persisters;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Linq;

namespace ArticleConsole.Crawlers
{
    public class ArticleCrawler : ICrawler
    {
        private readonly static int BATCH_SIZE = 100;

        private readonly ArticleConfig _config;
        private readonly HttpClient _httpClient;
        private readonly IPersister _persister;
        private readonly ILogger _logger;

        private readonly ActionBlock<Article> _workerBlock;

        public ArticleCrawler(ArticleConfig config, IPersister persister, IHttpClientFactory clientFactory, ILogger logger)
        {
            _config = config;
            _persister = persister;
            _httpClient = clientFactory.CreateClient(Constants.HTTP_CLIENT_NAME_DEFAULT);
            _logger = logger;

            _workerBlock = new ActionBlock<Article>(
                PopulateArticleAsync,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism
                });
        }

        public async Task ExecuteAsync()
        {
            await CrawlCatalogsAsync();
            await CrawlArticlesAsync();
        }

        private async Task CrawlCatalogsAsync()
        {
            var previous = _persister.GetPrevious(_config.FeedSource);

            var previousLink = previous?.Url;

            if (string.IsNullOrEmpty(previousLink))
            {
                _logger.LogInformation("Crawling {0} feed catalogs fully", _config.FeedSource);
            }
            else
            {
                _logger.LogInformation("Crawling {0} feed catalogs from last article: {1}", _config.FeedSource, previousLink);
            }

            var articles = new List<Article>();

            var page = _config.FeedPageIndexStart;
            string feedUrl = null;
            bool exceedPrevious = false;
            do
            {
                try
                {
                    feedUrl = _config.FeedUrl.Contains("{0}") ? string.Format(_config.FeedUrl, page) : _config.FeedUrl;

                    _logger.LogDebug("Crawling {0} feed catalogs: {1}", _config.FeedSource, feedUrl);

                    var content = await _httpClient.GetStringAsync(feedUrl);

                    var feedHtmlDoc = new HtmlDocument();
                    feedHtmlDoc.LoadHtml(content);

                    var feedHtmlNav = feedHtmlDoc.CreateNavigator();

                    var feedItemLinkIterator = feedHtmlNav.Select(_config.FeedItemLink);

                    // exit as no item detected
                    if (feedItemLinkIterator.Count == 0)
                    {
                        break;
                    }

                    while (feedItemLinkIterator.MoveNext())
                    {
                        var articleLink = feedItemLinkIterator.Current.Value?.Trim();
                        if (!string.IsNullOrEmpty(articleLink))
                        {
                            articleLink = Utilities.ResolveResourceUrl(articleLink, feedUrl);
                            if (articleLink.Equals(previousLink, StringComparison.CurrentCultureIgnoreCase))
                            {
                                exceedPrevious = true;
                                break;
                            }

                            articles.Add(new Article
                            {
                                Url = articleLink,
                                Source = _config.FeedSource
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve {0} feed catalogs: {1}", _config.FeedSource, feedUrl);

                    // exit without saving
                    return;
                }

                if (_config.FeedUrl.Contains("{0}"))
                {
                    page++;
                }
            } while (page >= 0
                && !exceedPrevious
                && (_config.FeedMaxPagesLimit == -1 || page < _config.FeedPageIndexStart + _config.FeedMaxPagesLimit)
            );

            _logger.LogInformation("Crawled {0} feed catalogs: {1} articles", _config.FeedSource, articles.Count);

            // record order isn't guaranteed in batch inset, so let's save the records one by one
            // save from the last one in case failure
            for (var i = articles.Count - 1; i >= 0; i--)
            {
                _persister.Add(articles[i]);

                if ((articles.Count - i) % 20 == 0 || i == 0)
                {
                    _logger.LogInformation("Persisting {0} feed catalogs articles: {1}/{2}", _config.FeedSource, articles.Count - i, articles.Count);
                }
            }

            _persister.Add(articles);

            _logger.LogInformation("Persisted {0} feed catalogs: {1} articles", _config.FeedSource, articles.Count);
        }

        private async Task<List<Article>> CrawlArticlesAsync()
        {
            int total = _persister.GetListCount(TransactionStatus.CreatedSummary, _config.FeedSource);
            int subTotal = 0;
            List<Article> articles = null;
            do
            {
                articles = _persister.GetList(TransactionStatus.CreatedSummary, BATCH_SIZE, articles?.LastOrDefault()?.Id, _config.FeedSource);

                foreach (var article in articles)
                {
                    _workerBlock.Post(article);
                }

                subTotal += articles.Count;
                _logger.LogInformation("Queuing {0} feed article details crawling: {1}/{2}", _config.FeedSource, subTotal, total);

                while (true)
                {
                    if (_workerBlock.InputCount < BATCH_SIZE)
                    {
                        break;
                    }

                    // wait for a while to avoid bulk of items in the queue/memory
                    Thread.Sleep(1000);
                }
            } while (articles.Count == BATCH_SIZE);

            _workerBlock.Complete();
            _workerBlock.Completion.Wait();

            _logger.LogInformation("Crawled {0} feed article details: {1} articles", _config.FeedSource, total);

            return articles;
        }

        private async Task PopulateArticleAsync(Article article)
        {
            _logger.LogDebug("Crawling {0} article: {1}", _config.FeedSource, article.Url);

            try
            {
                var content = await _httpClient.GetStringAsync(article.Url);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(content);

                var htmlNav = htmlDoc.CreateNavigator();

                article.Title = htmlNav.GetValue(_config.ArticleTitle);
                article.Published = htmlNav.GetValue<DateTime>(_config.ArticlePublished);
                article.Image = htmlNav.GetValue(_config.ArticleImage);
                article.Summary = htmlNav.GetValue(_config.ArticleSummary);
                article.Content = htmlNav.GetInnerHTML(_config.ArticleContent);
                // the following might have multiple match results
                article.Authors = ValueConverter.Join(htmlNav.GetValues(_config.ArticleAuthor), "; ");
                article.Keywords = ValueConverter.Join(htmlNav.GetValues(_config.ArticleKeywords), "; ");

                article.Status = TransactionStatus.CrawlingCompleted;

                _persister.Update(article);
            }
            catch (HttpRequestException hrex)
            {
                _logger.LogError("Failed to retrieve {0} article after max retries: {1}. {2}", _config.FeedSource, article.Url, hrex.Message);

                article.Status = TransactionStatus.CrawlingFailed;
                article.Notes = hrex.Message;

                _persister.Update(article);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve {0} article: {1}.", _config.FeedSource, article.Url);
            }
        }
    }
}
