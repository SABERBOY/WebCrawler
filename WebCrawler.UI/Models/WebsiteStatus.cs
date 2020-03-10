using System.ComponentModel.DataAnnotations;

namespace WebCrawler.UI.Models
{
    public enum WebsiteStatus
    {
        /// <summary>
        /// For filtering only
        /// </summary>
        [Display(Name = "All")]
        All = 0,
        [Display(Name = "Normal")]
        Normal = 1,
        [Display(Name = "Warning: No Dates")]
        WarningNoDates = 2,
        [Display(Name = "Error: Broken")]
        ErrorBroken = 3,
        [Display(Name = "Error: Catalog Missing")]
        ErrorCatalogMissing = 4,
        [Display(Name = "Error: Outdate")]
        ErrorOutdate = 5
    }
}
