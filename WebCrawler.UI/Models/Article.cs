using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.UI.Models
{
    [Table("atc_articles")]
    public class Article
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int WebsiteId { get; set; }
        public string Url { get; set; }
        public string ActualUrl { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ContentHtml { get; set; }
        public DateTime? Published { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
