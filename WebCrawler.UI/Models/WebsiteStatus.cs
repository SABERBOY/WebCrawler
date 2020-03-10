using System.ComponentModel.DataAnnotations;

namespace WebCrawler.UI.Models
{
    public enum WebsiteStatus
    {
        [Display(Name = "Normal")]
        Normal = 0,
        [Display(Name = "Warning: No Dates")]
        WarningNoDates = 1,
        [Display(Name = "Error: Broken")]
        ErrorBroken = 2,
        [Display(Name = "Error: Catalog Missing")]
        ErrorCatalogMissing = 3,
        [Display(Name = "Error: Outdate")]
        ErrorOutdate = 4
    }
}
