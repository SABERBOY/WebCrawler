using Microsoft.Extensions.Logging;
using System;
using WebCrawler.Housing.Crawlers;

namespace WebCrawler.Housing
{
    public class Worker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICrawler _crawler;
        private readonly ILogger _logger;

        public Worker(IServiceProvider serviceProvider, ICrawler crawler, ILogger logger)
        {
            _serviceProvider = serviceProvider;
            _crawler = crawler;
            _logger = logger;
        }

        public void Run()
        {
            _crawler.ExecuteAsync().Wait();
        }
    }
}
