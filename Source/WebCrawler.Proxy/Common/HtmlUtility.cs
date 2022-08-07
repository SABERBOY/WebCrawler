using System.Text.RegularExpressions;

namespace WebCrawler.Proxy.Common
{
    public static class HtmlUtility
    {
        public static string TrimHtmlTags(string html)
        {
            return Regex.Replace(html, @"<[^<>]+>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }
    }
}
