using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.UI.Models
{
    [Table("atc_crawllogs")]
    public class CrawlLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int WebsiteId { get; set; }
        public int CrawlId { get; set; }
        public string LastHandled { get; set; }
        public int Success { get; set; }
        public int Fail { get; set; }
        public string Notes { get; set; }
        [Column(TypeName = "ENUM")]
        public CrawlStatus Status { get; set; }
        public DateTime? Crawled { get; set; }


        [ForeignKey("WebsiteId")]
        public Website Website { get; set; }

        [ForeignKey("CrawlId")]
        public Crawl Crawl { get; set; }
    }
}
