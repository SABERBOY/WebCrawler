using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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
        public async static Task<ResponseData> GetHtmlAsync(this HttpClient httpClient, string requestUri)
        {
            var data = new ResponseData { RequestUrl = requestUri };

            using (var response = await httpClient.GetAsync(requestUri))
            {
                response.EnsureSuccessStatusCode();

                data.ActualUrl = response.RequestMessage.RequestUri.AbsoluteUri;

                var charset = response.Content.Headers.ContentType?.CharSet;
                Encoding encoding = charset == null ? null : Encoding.GetEncoding(charset);

                using (var stream = await response.Content.ReadAsStreamAsync())
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
                        using (var reader = new StreamReader(ms, encoding))
                        {
                            data.Content = await reader.ReadToEndAsync();
                            data.Content = Utilities.ResolveUrls(data.Content, requestUri);

                            if (encoding != null)
                            {
                                // use the response content type encoding if present
                                return data;
                            }

                            charset = DetectCharSet(data.Content);
                            if (string.IsNullOrEmpty(charset))
                            {
                                // skip as chartset doesn't present
                                return data;
                            }

                            encoding = Encoding.GetEncoding(charset);
                            if (encoding == reader.CurrentEncoding)
                            {
                                // skip as it's already the charset encoding
                                return data;
                            }

                            ms.Seek(0, SeekOrigin.Begin);
                            using (var reader2 = new StreamReader(ms, encoding))
                            {
                                // re-parse with the charset encoding
                                data.Content = await reader2.ReadToEndAsync();
                                data.Content = Utilities.ResolveUrls(data.Content, requestUri);

                                return data;
                            }
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

            var charset = m.Success ? m.Groups[1].Value : string.Empty;

            // charset correction
            return charset.Equals("utf8", StringComparison.CurrentCultureIgnoreCase) ? "utf-8" : charset;
        }

        #endregion

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action?.Invoke(item);
            }
        }

        public static string GetDisplayName(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    var attr = Attribute.GetCustomAttribute(field, typeof(DisplayAttribute)) as DisplayAttribute;

                    return attr?.Name;
                }
            }
            return null;
        }

        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    var attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

                    return attr?.Description;
                }
            }
            return null;
        }

        public static string GetAggregatedMessage(this AggregateException aex)
        {
            return null;
        }

        public static void HandleParseErrorsIfAny(this HtmlDocument htmlDoc, Action<string> action)
        {
            var parseErrors = string.Join("\r\n", htmlDoc.ParseErrors.Select(o => $"Html parsing error in line {o.Line} column {o.LinePosition}: {o.Reason}"));

            if (!string.IsNullOrEmpty(parseErrors))
            {
                action?.Invoke(parseErrors);
            }
        }
    }

    public class ResponseData
    {
        public string RequestUrl { get; set; }
        public string ActualUrl { get; set; }
        public string Content { get; set; }

        public bool IsRedirected
        {
            get
            {
                return !RequestUrl.Equals(ActualUrl, StringComparison.CurrentCultureIgnoreCase);
            }
        }

        public bool IsRedirectedExcludeHttps
        {
            get
            {
                var regex = new Regex("https?://", RegexOptions.IgnoreCase);

                return !regex.Replace(RequestUrl, "")
                    .Equals(regex.Replace(ActualUrl, ""), StringComparison.CurrentCultureIgnoreCase);
            }
        }
    }
}
