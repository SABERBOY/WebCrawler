using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Linq;

namespace ArticleConsole.Translators
{
    public class BaiduTranslator : ITranslator
    {
        private const string API_URL = "https://fanyi-api.baidu.com/api/trans/vip/translate";
        private const string LANGUAGE_FROM = "en";
        private const string LANGUAGE_TO = "zh";

        private readonly string _appId;
        private readonly string _appSecret;
        private readonly int _maxUTF8BytesPerRequest;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public BaiduTranslator(string appId, string appSecret, int maxUTF8BytesPerRequest, HttpClient httpClient, ILogger logger)
        {
            _appId = appId;
            _appSecret = appSecret;
            _maxUTF8BytesPerRequest = maxUTF8BytesPerRequest;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string[]> ExecuteAsync(params string[] content)
        {
            int[] blockPositions;
            var batches = TranslatorUtility.Wrap(content, _maxUTF8BytesPerRequest, out blockPositions);

            //var batches = new string[] { "“It has to be intact, this is a \"test\"”</h2>xxxx<article>hello, we are the world" };

            var outputs = new List<string>();
            string query;
            foreach (var batch in batches)
            {
                query = string.Join('\n', batch);

                Random rd = new Random();
                string salt = rd.Next(100000).ToString();
                string sign = EncryptString(_appId + query + salt + _appSecret);
                var requestUrl = $"{API_URL}?q={HttpUtility.UrlEncode(query)}&from={LANGUAGE_FROM}&to={LANGUAGE_TO}&appid={_appId}&salt={salt}&sign={sign}";

                var data = await _httpClient.GetStringAsync(requestUrl);

                var jo = JObject.Parse(data);

                outputs.AddRange(
                        jo["trans_result"].Select(o => Uri.UnescapeDataString(o.Value<string>("dst")))
                    );
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
