using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.Models
{
    [Table("atc_crawls")]
    public class Crawl
    {
        [Column("id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("success")]
        public int Success { get; set; }
        [Column("fail")]
        public int Fail { get; set; }
        [Column("notes")]
        public string? Notes { get; set; }
        [Column("status", TypeName = "varchar")]
        public CrawlStatus Status { get; set; }
        [Column("started")]
        public DateTime Started { get; set; }
        [Column("completed")]
        public DateTime? Completed { get; set; }
    }
}
