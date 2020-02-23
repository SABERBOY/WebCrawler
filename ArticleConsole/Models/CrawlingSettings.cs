using System.Collections.Generic;

namespace ArticleConsole.Models
{
    public class CrawlingSettings
    {
        public int HttpErrorRetry { get; set; }
        public int HttpErrorRetrySleep { get; set; }
        public List<ArticleConfig> Crawlers { get; set; }
    }
}
