namespace WebCrawler.Crawlers
{
    public class CrawlSettings
    {
        public int MaxDegreeOfParallelism { get; set; }
        public int FeedMaxPagesLimit { get; set; }
        public int OutdateDaysAgo { get; set; }
        public int MaxAcceptedBrokenDays { get; set; }
        public int HttpClientTimeout { get; set; }
    }
}
