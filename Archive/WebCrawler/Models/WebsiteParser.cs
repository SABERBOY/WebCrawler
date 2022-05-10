using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.Models
{
    [Table("ptf_website_parser")]
    public class WebsiteParser
    {
        [Column("parser_id")]
        public int Id { get; set; }
        [Column("website_id")]
        public int WebsiteId { get; set; }
        [Column("list_path")]
        public string ListPath { get; set; }

        [ForeignKey("WebsiteId")]
        public Website Website { get; set; }
    }
}
