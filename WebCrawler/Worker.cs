using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WebCrawler.Crawlers;
using WebCrawler.Persisters;

namespace WebCrawler
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
            var persister = _serviceProvider.GetRequiredService<IPersister>();

            var crawler = new ArticleCrawler(_crawlingSettings, persister, _clientFactory, _logger);

            crawler.ExecuteAsync().Wait();
        }
    }
}
