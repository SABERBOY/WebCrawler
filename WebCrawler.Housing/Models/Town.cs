using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.Housing.Models
{
    [Table("housing_towns")]
    public class Town
    {
        [Key]
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
