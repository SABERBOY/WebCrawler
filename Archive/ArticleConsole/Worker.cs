using ArticleConsole.Crawlers;
using ArticleConsole.Persisters;
using ArticleConsole.Translators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ArticleConsole
{
    public class Worker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CrawlingSettings _crawlingSettings;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger _logger;

        private readonly List<Task> _tasks;

        public Worker(IServiceProvider serviceProvider, CrawlingSettings crawlingSettings, IHttpClientFactory clientFactory, ILogger logger)
        {
            _serviceProvider = serviceProvider;
            _crawlingSettings = crawlingSettings;
            _clientFactory = clientFactory;
            _logger = logger;

            _tasks = new List<Task>();
        }

        public void Run()
        {
            if (_tasks.Count > 0)
            {
                throw new Exception("Worker is already running.");
            }

            foreach (var config in _crawlingSettings.Crawlers)
            {
                _tasks.Add(RunCrawlerAsync(config));
            }

            Task.WaitAll(_tasks.ToArray());

            RunTranslatorAsync().Wait();
        }

        private async Task RunCrawlerAsync(ArticleConfig config)
        {
            var persister = _serviceProvider.GetRequiredService<IPersister>();

            var crawler = new ArticleCrawler(config, persister, _clientFactory, _logger);

            await crawler.ExecuteAsync();
        }

        private async Task RunTranslatorAsync()
        {
            var translator = _serviceProvider.GetRequiredService<ITranslator>();

            await translator.ExecuteAsync();
        }
    }
}
