using ArticleConsole.Crawlers;
using ArticleConsole.Models;
using ArticleConsole.Persisters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ArticleConsole.Translators;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace ArticleConsole
{
    public class Worker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly CrawlingSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        private readonly List<Task> _tasks;

        public Worker(IServiceProvider serviceProvider, IConfiguration configuration, IOptions<CrawlingSettings> options, HttpClient httpClient, ILogger logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _settings = options.Value;
            _httpClient = httpClient;
            _logger = logger;

            _tasks = new List<Task>();
        }

        public void Run()
        {
            if (_tasks.Count > 0)
            {
                throw new Exception("Worker is already running.");
            }

            foreach (var config in _settings.Crawlers)
            {
                _tasks.Add(RunCrawlerAsync(config));
            }

            Task.WaitAll(_tasks.ToArray());

            RunTranslatorAsync().Wait();
        }

        private async Task RunCrawlerAsync(ArticleConfig config)
        {
            // get thread specific db context instance
            var dbContext = _serviceProvider.GetRequiredService<ArticleDbContext>();

            var persister = new MySqlPersister(dbContext, _logger);

            var previous = await persister.GetPreviousAsync(config.FeedSource);

            var crawler = new ArticleCrawler(config, _httpClient, _logger);

            var articles = await crawler.ExecuteAsync(previous);

            await persister.PersistAsync(articles, config.FeedSource);
        }

        private async Task RunTranslatorAsync()
        {
            var appId = _configuration["Translation:BaiduTranslator:AppId"];
            var appSecret = _configuration["Translation:BaiduTranslator:AppSecret"];
            var maxUTF8BytesPerRequest = int.Parse(_configuration["Translation:BaiduTranslator:MaxUTF8BytesPerRequest"]);
            var pausePerRequest = int.Parse(_configuration["Translation:BaiduTranslator:PausePerRequest"]);

            // get thread specific db context instance
            var dbContext = _serviceProvider.GetRequiredService<ArticleDbContext>();

            var persister = new MySqlPersister(dbContext, _logger);

            var untranslated = await persister.GetUnTranslatedAsync();

            var translator = new BaiduTranslator(appId, appSecret, maxUTF8BytesPerRequest, _httpClient, _logger);
            foreach (var article in untranslated)
            {
                var translated = await translator.ExecuteAsync(article.Title, article.Keywords, article.Summary, article.Content);

                await persister.PersistAsync(new ArticleZH
                {
                    Id = article.Id,
                    Source = article.Source,
                    Url = article.Url,
                    Image = article.Image,
                    Published = article.Published,
                    Authors = article.Authors,
                    Title = translated[0],
                    Keywords = translated[1],
                    Summary = translated[2],
                    Content = translated[3],
                    Timestamp = DateTime.Now
                });

                Thread.Sleep(pausePerRequest);
            }
        }
    }
}
