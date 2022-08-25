using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.Models
{
    [Table("atc_websites")]
    public class Website
    {
        [Column("id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("rank")]
        public int Rank { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("home")]
        public string? Home { get; set; }

        [Column("urlformat")]
        public string? UrlFormat { get; set; }

        [Column("startindex")]
        public int? StartIndex { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("registered")]
        public DateTime Registered { get; set; }

        [Column("enabled")]
        public bool Enabled { get; set; }

        [Column("validatedate")]
        public bool ValidateDate { get; set; }

        /// <summary>
        /// Value Conversions
        /// https://docs.microsoft.com/en-us/ef/core/modeling/value-conversions
        /// </summary>
        [Column("status", TypeName = "varchar")]
        public WebsiteStatus Status { get; set; }

        [Column("sysnotes")]
        public string? SysNotes { get; set; }

        public List<WebsiteRule>? Rules { get; set; }

        public List<CrawlLog>? CrawlLogs { get; set; }
    }
}
