using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.Models
{
    [Table("atc_crawllogs")]
    public class CrawlLog
    {
        [Column("id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("websiteid")]
        public int WebsiteId { get; set; }
        [Column("crawlid")]
        public int CrawlId { get; set; }
        [Column("lasthandled")]
        public string? LastHandled { get; set; }
        [Column("success")]
        public int Success { get; set; }
        [Column("fail")]
        public int Fail { get; set; }
        [Column("notes")]
        public string? Notes { get; set; }
        [Column("status", TypeName = "varchar")]
        public CrawlStatus Status { get; set; }
        [Column("crawled")]
        public DateTime? Crawled { get; set; }


        // NOTES: use Fluent API instead as the ForeignKey attribute doesn't appear working
        //[ForeignKey("websiteid")]
        public Website Website { get; set; }

        // NOTES: use Fluent API instead as the ForeignKey attribute doesn't appear working
        //[ForeignKey("crawlid")]
        public Crawl Crawl { get; set; }
    }
}
