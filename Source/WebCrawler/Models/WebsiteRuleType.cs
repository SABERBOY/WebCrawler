using System.ComponentModel.DataAnnotations;

namespace WebCrawler.Models
{
    public enum WebsiteRuleType
    {
        [Display(Name = "Catalog")]
        Catalog = 0,

        [Display(Name = "Article")]
        Article = 1,

        //[Display(Name = "Paging")]
        //Paging = 2
    }
}
