namespace WebCrawler.UI.Models
{
    public enum CrawlStatus
    {
        /// <summary>
        /// For filtering only
        /// </summary>
        All = 0,
        Queued = 1,
        Crawling = 2,
        Committing = 3,
        Completed = 4,
        Failed = 5,
        Cancelled = 6
    }
}
