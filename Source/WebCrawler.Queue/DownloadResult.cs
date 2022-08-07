using System.Net;
using System.Text;

namespace WebCrawler.Queue
{
    public class DownloadResult
    {
        public string RequestUri { get; set; }
        public string Content { get; set; }
        public string ContentType { get; set; }
        public Encoding Encoding { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string ResponseUri { get; set; }
        public Exception Exception { get; set; }
    }
}
