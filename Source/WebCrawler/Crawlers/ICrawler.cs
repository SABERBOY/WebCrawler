using Microsoft.Extensions.Logging;
using WebCrawler.Models;

namespace WebCrawler.Crawlers
{
    public interface ICrawler
    {
        Task ExecuteAsync(bool continuePrevious = false);
        event EventHandler<WebsiteCrawlingEventArgs> WebsiteCrawling;
        event EventHandler<CrawlCompletedEventArgs> Completed;
        event EventHandler<CrawlMessagingEventArgs> Messaging;
        event EventHandler<CrawlProgressChangedEventArgs> StatusChanged;
    }

    public class WebsiteCrawlingEventArgs : EventArgs
    {
        public CrawlLog CrawlLog { get; set; }

        public string Status { get; set; }

        public WebsiteCrawlingEventArgs(CrawlLog crawlLog)
            : base()
        {
            CrawlLog = crawlLog;
        }
    }

    public class CrawlCompletedEventArgs : EventArgs
    {
        public Crawl Crawl { get; set; }

        public string Status { get; set; }

        public CrawlCompletedEventArgs(Crawl crawl)
            : base()
        {
            Crawl = crawl;
        }
    }

    public class CrawlMessagingEventArgs : EventArgs
    {
        public string Message { get; set; }

        public string Url { get; set; }

        public LogLevel Level { get; set; }

        public CrawlMessagingEventArgs(string message, string url = null, LogLevel level = LogLevel.Information)
            : base()
        {
            Message = message;
            Url = url;
            Level = level;
        }
    }

    public class CrawlProgressChangedEventArgs : EventArgs
    {
        public int Success { get; set; }

        public int Fail { get; set; }

        public int Total { get; set; }

        public CrawlProgressChangedEventArgs(int success, int fail, int? total = null)
            : base()
        {
            Success = success;
            Fail = fail;

            if (total != null)
            {
                Total = total.Value;
            }
        }
    }
}
