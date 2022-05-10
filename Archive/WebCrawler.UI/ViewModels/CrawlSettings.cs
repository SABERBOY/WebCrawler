namespace WebCrawler.UI.ViewModels
{
    public class CrawlSettings
    {
        public int MaxDegreeOfParallelism { get; set; }
        public int FeedMaxPagesLimit { get; set; }
        public int OutdateDaysAgo { get; set; }
    }
}
