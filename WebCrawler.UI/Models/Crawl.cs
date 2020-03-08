using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.UI.Models
{
    [Table("atc_crawls")]
    public class Crawl
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }
        public string Notes { get; set; }
        [Column(TypeName = "ENUM")]
        public CrawlStatus Status { get; set; }
        public DateTime Started { get; set; }
        public DateTime? Completed { get; set; }
    }
}
