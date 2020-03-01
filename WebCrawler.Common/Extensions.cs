using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace WebCrawler.Common
{
    public static class Extensions
    {
        #region XPathNavigator Extensions

        public static string GetValue(this XPathNavigator xNav, string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
            {
                return string.Empty;
            }

            var pathValue = xNav.SelectSingleNode(xpath);

            return pathValue?.Value;
        }

        public static string[] GetValues(this XPathNavigator xNav, string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
            {
                return new string[0];
            }

            var iterator = xNav.Select(xpath);

            List<string> values = new List<string>();

            while (iterator.MoveNext())
            {
                values.Add(iterator.Current.Value);
            }

            return values.ToArray();
        }

        public static T GetValue<T>(this XPathNavigator xNav, string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
            {
                return default(T);
            }

            var pathValue = xNav.SelectSingleNode(xpath);

            return ValueConverter.Parse<T>(pathValue?.Value);
        }

        public static T[] GetValues<T>(this XPathNavigator xNav, string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
            {
                return new T[0];
            }

            var iterator = xNav.Select(xpath);

            List<string> values = new List<string>();

            while (iterator.MoveNext())
            {
                values.Add(iterator.Current.Value);
            }

            return values.Select(o => ValueConverter.Parse<T>(o)).ToArray();
        }

        public static string GetInnerHTML(this XPathNavigator xNav, string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
            {
                return string.Empty;
            }

            var pathValue = xNav.SelectSingleNode(xpath);

            return pathValue?.InnerXml;
        }

        #endregion

        #region HttpClient

        /// <summary>
        /// Read HTML with auto-detected encoding.
        /// </summary>
        /// <returns></returns>
        public async static Task<string> GetHTMLAsync(this HttpClient httpClient, string requestUri)
        {
            using (var stream = await httpClient.GetStreamAsync(requestUri))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] buffer = new byte[1024];
                    int length = stream.Read(buffer, 0, buffer.Length);
                    while (length > 0)
                    {
                        ms.Write(buffer, 0, length);
                        length = stream.Read(buffer, 0, buffer.Length);
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(ms))
                    {
                        var html = await reader.ReadToEndAsync();

                        var charset = DetectCharSet(html);
                        if (string.IsNullOrEmpty(charset))
                        {
                            return html;
                        }

                        // charset correction
                        if (charset.Equals("utf8", StringComparison.CurrentCultureIgnoreCase))
                        {
                            charset = "utf-8";
                        }

                        Encoding encoding = Encoding.GetEncoding(charset);
                        if (encoding == reader.CurrentEncoding)
                        {
                            return html;
                        }

                        ms.Seek(0, SeekOrigin.Begin);
                        using (var reader2 = new StreamReader(ms, encoding))
                        {
                            return await reader2.ReadToEndAsync();
                        }
                    }
                }
            }
        }

        private static string DetectCharSet(string rawContent)
        {
            Match m = Regex.Match(rawContent, @"\<meta [^<>]+charset= *([\w-]+)", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                return m.Groups[1].Value;
            }

            m = Regex.Match(rawContent, @"\<meta [^<>]*charset=[^\w]? *([\w-]+)", RegexOptions.IgnoreCase);

            return m.Success ? m.Groups[1].Value : string.Empty;
        }

        #endregion

        public static string GetAggregatedMessage(this AggregateException aex)
        {
            return null;
        }
    }
}
