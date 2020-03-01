using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.UI.Models
{
    [Table("atc_websites")]
    public class Website
    {
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

        public List<CrawlLog> CrawlLogs { get; set; }
    }
}
