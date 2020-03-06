using System.ComponentModel.DataAnnotations;

namespace WebCrawler.UI.Models
{
    public enum WebsiteStatus
    {
        [Display(Name = "Normal")]
        Normal = 0,
        [Display(Name = "Broken")]
        Broken = 1,
        [Display(Name = "Catalog Missing")]
        CatalogMissing = 2,
        [Display(Name = "Outdate")]
        Outdate = 3
    }
}
