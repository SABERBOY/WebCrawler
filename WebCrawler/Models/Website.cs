using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.Models
{
    [Table("ptf_website")]
    public class Website
    {
        [Column("website_id")]
        public int Id { get; set; }
        [Column("web_name")]
        public string Name { get; set; }
        [Column("url_home")]
        public string Home { get; set; }
        [Column("url")]
        public string Url { get; set; }
        [Column("rank")]
        public int Rank { get; set; }
        [Column("previous")]
        public string Previous { get; set; }
        [Column("failures")]
        public int Failures { get; set; }
        [Column("enabled")]
        public bool Enabled { get; set; }
    }
}
