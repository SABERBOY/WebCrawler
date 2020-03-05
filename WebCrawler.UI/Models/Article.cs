using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.UI.Models
{
    [Table("articles_generic")]
    public class Article
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int WebsiteId { get; set; }
        [Required]
        [MaxLength(512)]
        public string Url { get; set; }
        [MaxLength(1024)]
        public string Title { get; set; }
        public string Content { get; set; }
        public string ContentHtml { get; set; }
        public DateTime? Published { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
