namespace ArticleConsole.Models
{
    public class ArticleConfig
    {
        public ArticleSource FeedSource { get; set; }
        public string FeedUrl { get; set; }
        public string FeedItemLink { get; set; }
        public int FeedPageIndexStart { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
        public string ArticleTitle { get; set; }
        public string ArticlePublished { get; set; }
        public string ArticleAuthor { get; set; }
        public string ArticleImage { get; set; }
        public string ArticleSummary { get; set; }
        public string ArticleContent { get; set; }
        public string ArticleKeywords { get; set; }
    }
}
