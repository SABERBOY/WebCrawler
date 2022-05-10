using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArticleConsole.Models
{
    [Table("articles")]
    public class Article : ArticleBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public new int Id { get; set; }
        public TransactionStatus Status { get; set; }
        public string Notes { get; set; }
    }
}
