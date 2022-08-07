using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.Models
{
    [Table("atc_articles")]
    public class Article
    {
        [Column("id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("websiteid")]
        public int WebsiteId { get; set; }
        [Column("url")]
        public string? Url { get; set; }
        [Column("actualurl")]
        public string? ActualUrl { get; set; }
        [Column("title")]
        public string? Title { get; set; }
        [Column("content")]
        public string? Content { get; set; }
        [Column("contenthtml")]
        public string? ContentHtml { get; set; }
        [Column("published")]
        public DateTime? Published { get; set; }
        [Column("timestamp")]
        public DateTime Timestamp { get; set; }
        [Column("author")]
        public string? Author { get; set; }
    }
}
