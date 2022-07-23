using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using WebCrawler.DTO;
using WebCrawler.Models;

namespace WebCrawler.Common
{
    public static class HtmlHelper
    {
        #region URL

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

            string baseUrl;
            if (baseTagMatch.Success)
            {
                baseUrl = baseTagMatch.Groups[1].Value;

                if (baseUrl.StartsWith("//"))
                {
                    baseUrl = new Uri(pageUrl).Scheme + ":" + baseUrl;
                }
                else if (baseUrl.StartsWith("/"))
                {
                    baseUrl = new Uri(new Uri(pageUrl), baseUrl).AbsoluteUri;
                }
            }
            else
            {
                baseUrl = pageUrl;
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

        #endregion

        /// <summary>
        /// Read HTML with auto-detected encoding.
        /// </summary>
        /// <returns></returns>
        public async static Task<ResponseData> GetHtmlAsync(string requestUri, HttpClient httpClient)
        {
            var data = new ResponseData { RequestUrl = requestUri };

            using (var response = await httpClient.GetAsync(requestUri))
            {
                response.EnsureSuccessStatusCode();

                data.ActualUrl = response.RequestMessage.RequestUri.AbsoluteUri;

                Encoding encoding = DetectEncoding(response.Content.Headers.ContentType?.CharSet);

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        await stream.CopyToAsync(ms);

                        ms.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(ms, encoding))
                        {
                            data.Content = await reader.ReadToEndAsync();
                            data.Content = ResolveUrls(data.Content, requestUri);

                            if (encoding != null)
                            {
                                // use the response content type encoding if present
                                return data;
                            }

                            encoding = DetectEncoding(data.Content);
                            if (encoding == null || encoding.EncodingName == reader.CurrentEncoding.EncodingName)
                            {
                                // skip the HTML meta encoding
                                return data;
                            }

                            ms.Seek(0, SeekOrigin.Begin);
                            using (var reader2 = new StreamReader(ms, encoding))
                            {
                                // re-parse with the charset encoding
                                data.Content = await reader2.ReadToEndAsync();
                                data.Content = ResolveUrls(data.Content, requestUri);

                                return data;
                            }
                        }
                    }
                }
            }
        }

        public async static Task<ResponseData> GetPageDataAsync(HttpClient httpClient, string pageUrl, WebsiteRuleDTO rule = null)
        {
            var pageLoadOption = rule == null ? PageLoadOption.Default : rule.PageLoadOption;

            switch (pageLoadOption)
            {
                case PageLoadOption.Default:
                    return await GetHtmlAsync(pageUrl, httpClient);
                case PageLoadOption.Redirection:
                    var dataUrl = Regex.Replace(pageUrl, rule.PageUrlReviseExp, rule.PageUrlReplacement, RegexOptions.IgnoreCase);

                    var httpMessage = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(dataUrl)
                    };
                    httpMessage.Headers.Add("Referer", pageUrl);
                    //httpMessage.Headers.Add("Cookie", "qgqp_b_id=45069803c0cba71d192e4a51a286490f; st_si=83006616874806; st_asi=delete; st_pvi=93353666965915; st_sp=2022-06-05%2020%3A28%3A20; st_inirUrl=https%3A%2F%2Fcaifuhao.eastmoney.com%2Fcfh%2F146462; st_sn=25; st_psi=20220619182640610-119101302131-3421560488");

                    using (var response = await httpClient.SendAsync(httpMessage))
                    {
                        response.EnsureSuccessStatusCode();

                        //Encoding encoding = DetectEncoding(response.Content.Headers.ContentType?.CharSet);

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                await stream.CopyToAsync(ms);

                                ms.Seek(0, SeekOrigin.Begin);
                                using (var reader = new StreamReader(ms))
                                {
                                    return new ResponseData
                                    {
                                        RequestUrl = pageUrl,
                                        ActualUrl = response.RequestMessage.RequestUri.AbsoluteUri,
                                        Content = await reader.ReadToEndAsync()
                                    };
                                    // TBD
                                    //resp.SubResponse.Content = ResolveUrls(resp.SubResponse.Content, dataUrl);
                                }
                            }
                        }
                    }
                case PageLoadOption.BrowserProxy:
                    throw new NotImplementedException();
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Trim spaces, tabs, linebreaks from the text in a HTML page.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            // trim start/end chars
            text = text.Trim('\r', '\n', '\t', ' ');

            // trim mid chars to single space
            text = Regex.Replace(text, @"[\r\n\t ]+", " ");

            // resolve code like &lt;
            text = HttpUtility.HtmlDecode(text);

            return text;
        }

        public static string NormalizeHtml(string html, bool stripBase64Image = false)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            // trim start/end chars
            html = html.Trim('\r', '\n', '\t', ' ');

            // trim mid chars to single space
            html = Regex.Replace(html, @"[\r\n\t ]+", " ");

            if (stripBase64Image)
            {
                html = Regex.Replace(html, @"(?<=src=['""])data:image/\w+;base64,[^'"" ]*", "", RegexOptions.IgnoreCase);
            }

            return html;
        }

        public static string TrimHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }

            var text = Regex.Replace(html, @"<[^<>]+>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return NormalizeText(text);
        }

        public static void HandleParseErrorsIfAny(HtmlDocument htmlDoc, Action<string> action)
        {
            var parseErrors = string.Join(" ", htmlDoc.ParseErrors.Select(o => $"Line {o.Line} column {o.LinePosition}: {o.Reason}."));

            if (!string.IsNullOrEmpty(parseErrors))
            {
                parseErrors = "Html parsing errors: " + parseErrors;

                action?.Invoke(parseErrors);
            }
        }

        public static string TrimJsonP(string content)
        {
            var match = Regex.Match(content, @"^[\w_]+\((.+)\);?$");

            return match.Success ? match.Groups[1].Value : content;
        }

        #region Private Members

        private static Encoding DetectEncoding(string rawContent)
        {
            if (string.IsNullOrEmpty(rawContent))
            {
                return null;
            }

            string charset;
            if (rawContent.Contains('<'))
            {
                Match m = Regex.Match(rawContent, @"\<meta [^<>]+charset= *([\w-]+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    charset = m.Groups[1].Value;
                }
                else
                {
                    m = Regex.Match(rawContent, @"\<meta [^<>]*charset=[^\w]? *([\w-]+)", RegexOptions.IgnoreCase);

                    charset = m.Success ? m.Groups[1].Value : string.Empty;
                }
            }
            else
            {
                charset = Regex.Match(rawContent, @"[a-zA-Z0-9-]{4,}").Value;
            }

            // charset correction
            if (charset.Equals("utf8", StringComparison.CurrentCultureIgnoreCase))
            {
                charset = "utf-8";
            }

            return string.IsNullOrEmpty(charset) ? null : Encoding.GetEncoding(charset);
        }

        #endregion
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
