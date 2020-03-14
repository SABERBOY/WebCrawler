using WebCrawler.Common;
using WebCrawler.Common.Analyzers;

namespace WebCrawler.UI.ViewModels
{
    public class TestResult
    {
        public ResponseData CatalogsResponse { get; set; }
        public CatalogItem[] Catalogs { get; set; }
    }
}
