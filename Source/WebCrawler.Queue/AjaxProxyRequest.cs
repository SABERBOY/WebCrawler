namespace WebCrawler.Queue
{
    public class AjaxProxyRequest
    {
        public string PageUrl { get; set; }
        public string AjaxUrlExp { get; set; }
        public int TimeoutSeconds { get; set; }
    }
}
