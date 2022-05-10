using WebCrawler.Analyzers;
using WebCrawler.Common;
using WebCrawler.Models;

namespace WebCrawler.WPF.ViewModels
{
    public class TestResult
    {
        public WebsiteStatus? Status { get; set; }
        public string Notes { get; set; }
        public ResponseData CatalogsResponse { get; set; }
        public CatalogItem[] Catalogs { get; set; }
    }
}
