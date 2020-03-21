using System;
using System.Text.RegularExpressions;

namespace WebCrawler.Common
{
    public static class Utilities
    {
        public static string GetRootSiteUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            Match m = Regex.Match(url, @"https?://([^/\r\n ]+)");
            if (m.Success)
            {
                return m.Value;
            }

            return url.ToLower();
        }

        public static string ResolveResourceUrl(string resourceUrl, string pageUrl)
        {
            return new Uri(new Uri(pageUrl), resourceUrl).AbsoluteUri;
        }

        public static string ResolveUrls(string html, string pageUrl)
        {
            var baseTagMatch = Regex.Match(html, @"(?is)<base +href=[""']?([^""' ]+)");

            var baseUrl = baseTagMatch.Success ? baseTagMatch.Groups[1].Value : pageUrl;
            if (baseUrl.StartsWith("//"))
            {
                baseUrl = new Uri(pageUrl).Scheme + ":" + baseUrl;
            }
            else if (baseUrl.StartsWith("/"))
            {
                baseUrl = new Uri(new Uri(pageUrl), baseUrl).AbsoluteUri;
            }

            Uri baseUri = new Uri(baseUrl);

            return Regex.Replace(html, @"(?is)(href|src)=((""|')([^""']+)\3|([^ ]+))", (match) =>
            {
                string org = match.Value;
                string link = !string.IsNullOrEmpty(match.Groups[4].Value) ? match.Groups[4].Value : match.Groups[5].Value;
                if (link.StartsWith("http"))
                {
                    return org;
                }

                try
                {
                    Uri thisUri = new Uri(baseUri, link);
                    return string.Format("{0}=\"{1}\"", match.Groups[1].Value, thisUri.AbsoluteUri);
                }
                catch (Exception)
                {
                    return org;
                }
            });
        }

        /// <summary>
        /// Trim spaces, tabs, linebreaks from the text in a HTML page.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string TrimHtmlText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // trim start/end chars
            text = text.Trim('\r', '\n', '\t', ' ');

            // trim mid chars to single space
            text = Regex.Replace(text, @"[\r\n\t ]+", " ");

            return text;
        }
    }
}
