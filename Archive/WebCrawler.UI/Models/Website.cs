using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.UI.Models
{
    [Table("atc_websites")]
    public class Website
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Rank { get; set; }
        public string Name { get; set; }
        public string Home { get; set; }
        public string UrlFormat { get; set; }
        public int? StartIndex { get; set; }
        public string ListPath { get; set; }
        public string Notes { get; set; }
        public DateTime Registered { get; set; }
        public bool Enabled { get; set; }
        public bool ValidateDate { get; set; }
        /// <summary>
        /// Value Conversions
        /// https://docs.microsoft.com/en-us/ef/core/modeling/value-conversions
        /// </summary>
        [Column(TypeName = "ENUM")]
        public WebsiteStatus Status { get; set; }
        public string SysNotes { get; set; }

        public List<CrawlLog> CrawlLogs { get; set; }
    }
}
