using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.UI.Models
{
    [Table("atc_crawllogs")]
    public class CrawlLog
    {
        public int Id { get; set; }
        public int WebsiteId { get; set; }
        public string Previous { get; set; }
        public string Notes { get; set; }
        public CrawlStatus Status { get; set; }
        public DateTime Crawled { get; set; }

        [ForeignKey("WebsiteId")]
        public Website Website { get; set; }
    }
}
