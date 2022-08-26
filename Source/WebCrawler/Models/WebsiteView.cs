using System.ComponentModel.DataAnnotations;

namespace WebCrawler.Models
{
    public enum WebsiteView
    {
        [Display(Name = "All")]
        Default = 0,
        [Display(Name = "Pending")]
        Pending = 1
    }
}
