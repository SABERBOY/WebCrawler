using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArticleConsole.Models
{
    public abstract class ArticleBase
    {
        [Key]
        public int Id { get; set; }
        public ArticleSource Source { get; set; }
        [Required]
        [MaxLength(512)]
        public string Url { get; set; }
        [MaxLength(1024)]
        public string Title { get; set; }
        [MaxLength(512)]
        public string Authors { get; set; }
        [MaxLength(512)]
        public string Keywords { get; set; }
        [MaxLength(512)]
        public string Image { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public DateTime Published { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
