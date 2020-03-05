namespace WebCrawler.UI.Crawlers
{
    public class CrawlingSettings
    {
        public int MaxDegreeOfParallelism { get; set; }
        public int FeedMaxPagesLimit { get; set; }
        public int OutdateDaysAgo { get; set; }
    }
}
