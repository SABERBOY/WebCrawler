using ArticleConsole.Models;
using ArticleConsole.Persisters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ArticleConsole.Translators
{
    public class BaiduTranslator : ITranslator
    {
        private readonly static int BATCH_SIZE = 100;

        private const string API_URL = "https://fanyi-api.baidu.com/api/trans/vip/translate";
        private const string LANGUAGE_FROM = "en";
        private const string LANGUAGE_TO = "zh";

        private readonly TranslationSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly IPersister _persister;
        private readonly ILogger _logger;

        public BaiduTranslator(TranslationSettings settings, IPersister persister, IHttpClientFactory clientFactory, ILogger logger)
        {
            _settings = settings;
            _persister = persister;
            _logger = logger;

            _httpClient = clientFactory.CreateClient();
        }

        public async Task ExecuteAsync()
        {
            int total = _persister.GetListCount(TransactionStatus.CrawlingCompleted);
            int subTotal = 0;
            List<Article> articles = null;
            do
            {
                articles = _persister.GetList(TransactionStatus.CrawlingCompleted, BATCH_SIZE, articles?.LastOrDefault()?.Id);

                foreach (var article in articles)
                {
                    _logger.LogDebug("Translating article: {0}", article.Url);

                    try
                    {
                        var translated = await TranslateAsync(article.Title, article.Keywords, article.Summary, article.Content);

                        _persister.Add(new ArticleZH
                        {
                            Id = article.Id,
                            Source = article.Source,
                            Url = article.Url,
                            Image = article.Image,
                            Published = article.Published,
                            Authors = article.Authors,
                            Title = translated[0],
                            Keywords = translated[1],
                            Summary = translated[2],
                            Content = translated[3],
                            Timestamp = DateTime.Now
                        });

                        article.Status = TransactionStatus.TranslationCompleted;
                        _persister.Update(article);
                    }
                    catch (Exception ex)
                    {
                        article.Status = TransactionStatus.TranslationFailed;
                        article.Notes = ex.Message;

                        _persister.Update(article);

                        _logger.LogError(ex, "Failed to translate article: {0}", article.Url);
                    }
                }

                subTotal += articles.Count;
                _logger.LogInformation("Translated articles: {0}/{1}", subTotal, total);
            } while (articles.Count == BATCH_SIZE);
        }

        public async Task<string[]> TranslateAsync(params string[] content)
        {
            int[] blockPositions;
            var batches = TranslatorUtility.Wrap(content, _settings.MaxUTF8BytesPerRequest, out blockPositions);

            //var batches = new string[] { "“It has to be intact, this is a \"test\"”</h2>xxxx<article>hello, we are the world" };

            var outputs = new List<string>();
            string query;
            foreach (var batch in batches)
            {
                query = string.Join('\n', batch);

                Random rd = new Random();
                string salt = rd.Next(100000).ToString();
                string sign = EncryptString(_settings.AppId + query + salt + _settings.AppSecret);
                var requestUrl = $"{API_URL}?q={HttpUtility.UrlEncode(query)}&from={LANGUAGE_FROM}&to={LANGUAGE_TO}&appid={_settings.AppId}&salt={salt}&sign={sign}";

                var data = await _httpClient.GetStringAsync(requestUrl);

                var jo = JObject.Parse(data);
                if (jo["error_code"] != null)
                {
                    throw new Exception(data);
                }
                else
                {
                    outputs.AddRange(jo["trans_result"].Select(o => Uri.UnescapeDataString(o.Value<string>("dst"))));
                }

                if (_settings.PausePerRequest > 0)
                {
                    Thread.Sleep(_settings.PausePerRequest);
                }
            }

            var translations = TranslatorUtility.Unwrap(outputs, blockPositions);

            return translations;
        }

        /// <summary>
        /// 计算MD5值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string EncryptString(string str)
        {
            MD5 md5 = MD5.Create();
            // 将字符串转换成字节数组
            byte[] byteOld = Encoding.UTF8.GetBytes(str);
            // 调用加密方法
            byte[] byteNew = md5.ComputeHash(byteOld);
            // 将加密结果转换为字符串
            StringBuilder sb = new StringBuilder();
            foreach (byte b in byteNew)
            {
                // 将字节转换成16进制表示的字符串，
                sb.Append(b.ToString("x2"));
            }
            // 返回加密的字符串
            return sb.ToString();
        }
    }
}
