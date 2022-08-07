using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace WebCrawler.Proxy.Common
{
    public static class Extensions
    {
        public static string GetContentType(this CoreWebView2WebResourceResponseView response)
        {
            return response.Headers.FirstOrDefault(o => o.Key.ToLower() == "content-type").Value?.Replace("\"", "") ?? "text/html";
        }

        public static bool IsHtml(this CoreWebView2WebResourceResponseView response)
        {
            return GetContentType(response).Contains("text/html");
        }

        public static async Task<string> GetContentAsync(this WebView2 webView2)
        {
            string html = await webView2.ExecuteScriptAsync("document.documentElement.outerHTML");

            if (string.IsNullOrEmpty(html))
            {
                return html;
            }

            html = Regex.Unescape(html);

            // page rendered as "View Source"
            if (html.Contains("<label class=\"line-wrap-control\">Line wrap<input type=\"checkbox\" aria-label=\"Line wrap\"></label>"))
            {
                html = HtmlUtility.TrimHtmlTags(html);

                html = HttpUtility.HtmlDecode(html);

                html = Regex.Replace(html, @"(^""Line wrap|""$)", "");
            }
            else
            {
                html = Regex.Replace(html, @"(^""|""$)", "");
            }

            return html;
        }
    }
}
