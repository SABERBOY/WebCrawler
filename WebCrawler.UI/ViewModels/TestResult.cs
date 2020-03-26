using WebCrawler.Common;
using WebCrawler.Common.Analyzers;
using WebCrawler.UI.Models;

namespace WebCrawler.UI.ViewModels
{
    public class TestResult
    {
        public WebsiteStatus? Status { get; set; }
        public string Notes { get; set; }
        public ResponseData CatalogsResponse { get; set; }
        public CatalogItem[] Catalogs { get; set; }
    }
}
