using ArticleConsole.Common;
using ArticleConsole.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ArticleConsole.Crawlers
{
    public class ArticleCrawler : ICrawler
    {
        private readonly ArticleConfig _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        private readonly ActionBlock<Article> _workerBlock;

        public ArticleCrawler(ArticleConfig config, HttpClient httpClient, ILogger logger)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;

            _workerBlock = new ActionBlock<Article>(
                async article =>
                {
                    try
                    {
                        await PopulateArticleAsync(article);
                    }
                    catch (HttpRequestException hrex)
                    {
                        _logger.LogError("Failed to retrieve {0} article after max retries: {1}. {2}", _config.FeedSource, article.Url, hrex.Message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to retrieve {0} article: {1}.", _config.FeedSource, article.Url);
                    }
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism
                });
        }

        public async Task<List<Article>> ExecuteAsync(Article previous = null)
        {
            var previousLink = previous?.Url;

            if (string.IsNullOrEmpty(previousLink))
            {
                _logger.LogInformation("Crawling {0} feed fully", _config.FeedSource);
            }
            else
            {
                _logger.LogInformation("Crawling {0} feed from last article: {1}", _config.FeedSource, previousLink);
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

                    _logger.LogDebug("Crawling {0} feed: {1}", _config.FeedSource, feedUrl);

                    var content = await _httpClient.GetStringAsync(feedUrl);

                    var feedHtmlDoc = new HtmlDocument();
                    feedHtmlDoc.LoadHtml(content);

                    var feedHtmlNav = feedHtmlDoc.CreateNavigator();

                    var feedItemLinkIterator = feedHtmlNav.Select(_config.FeedItemLink);
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

                            var article = new Article { Url = articleLink, Source = _config.FeedSource };
                            articles.Add(article);

                            _workerBlock.Post(article);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve {0} feed: {1}", _config.FeedSource, feedUrl);

                    break;
                }

                if (_config.FeedUrl.Contains("{0}"))
                {
                    page++;
                }
            } while (page >= 0 && !exceedPrevious);

            _workerBlock.Complete();
            _workerBlock.Completion.Wait();

            _logger.LogInformation("Crawled {0} feed: {1} articles", _config.FeedSource, articles.Count);

            return articles;
        }

        private async Task PopulateArticleAsync(Article article)
        {
            _logger.LogDebug("Crawling {0} article: {1}", _config.FeedSource, article.Url);

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
        }
    }
}
