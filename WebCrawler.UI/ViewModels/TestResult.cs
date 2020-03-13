using WebCrawler.Common.Analyzers;

namespace WebCrawler.UI.ViewModels
{
    public class TestResult
    {
        public bool Redirected { get; set; }
        public CatalogItem[] Catalogs { get; set; }
    }
}
