using ArticleConsole.Models;
using ArticleConsole.Translators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Linq;

namespace ArticleConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //using (HttpClient client = new HttpClient())
            //{
            //    //var text = client.GetStringAsync("https://www.cell.com/cell/fulltext/S0092-8674(20)30112-4").Result;
            //    var text = ""

            //    var result = new BaiduTranslator("20200220000386370", "BFBYCHmQtWV011qK9aRE", new HttpClient(), null).ExecuteAsync(text).Result;

            //    //var test = Regex.Matches(text, @".*?<(div|p|br|h\d|figure|section|aside|header|footer|blockquote|ul)([ /][<>]*)?>", RegexOptions.IgnoreCase);

            //    //var text2 = string.Join("", test.Select(o => o.Value));
            //    ;
            //}

            //return;

            //var str = "abcdefghijk<p>lmnopqrstuvwxyz./?[]p\'()8&^%$#!@()";
            //StringBuilder builder = new StringBuilder();
            //for (var i = 0; i < 100000; i++)
            //{
            //    builder.Append(str + i);
            //}
            //str = builder.ToString();

            //DateTime start = DateTime.Now;

            //int len = 0;
            //var x = Regex.Matches(str, @"</?(div|p|br|h\d|figure|section|aside|header|footer|blockquote|ul)([ /][<>]*)?>", RegexOptions.IgnoreCase);
            ////var x = Regex.Match(str, @"</?(div|p|br|h\d|figure|section|aside|header|footer|blockquote|ul)([ /][<>]*)?>", RegexOptions.IgnoreCase);
            ////while (x.Success)
            ////{
            ////    len++;
            ////    x = x.NextMatch();
            ////}

            //DateTime end = DateTime.Now;

            ////len = x.Count;
            //Console.WriteLine(len);
            //Console.WriteLine(end.Subtract(start));

            //;

            //return;

            //using (HttpClient client = new HttpClient())
            //{
            //    var text = client.GetStringAsync("https://www.cell.com/cell/fulltext/S0092-8674(20)30112-4").Result;
            //    var test = Regex.Matches(text, @".*?<(div|p|br|h\d|figure|section|aside|header|footer|blockquote|ul)([ /][<>]*)?>", RegexOptions.IgnoreCase);

            //    var text2 = string.Join("", test.Select(o => o.Value));
            //    ;
            //}


            //return;

    

            try
            {
                var services = new ServiceCollection();

                ConfigureServices(services);

                using (var serviceProvider = services.BuildServiceProvider())
                {
                    serviceProvider.GetService<Worker>().Run();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Press any key to close this window");
            Console.ReadKey();
        }

        static void ConfigureServices(IServiceCollection services)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            services.AddSingleton<IConfiguration>(config);

            services.AddOptions();
            services.Configure<CrawlingSettings>(config.GetSection("Crawling"));

            //services.AddSingleton<HttpClient>();

            // register as Transient, because efcore dbcontext isn't thread safe
            // https://docs.microsoft.com/en-us/ef/core/miscellaneous/configuring-dbcontext#avoiding-dbcontext-threading-issues
            //services.AddTransient<IPersister, MySqlPersister>();
            // configure db context
            services.AddDbContext<ArticleDbContext>(options => options.UseMySql(config["ConnectionStrings:MySqlConnection"]), ServiceLifetime.Transient, ServiceLifetime.Transient);
            //services.AddDbContextPool<ArticleDbContext>(options => options.UseMySql(config["ConnectionStrings:MySqlConnection"]));
            
            var settings = new CrawlingSettings();
            config.Bind("Crawling", settings);

            // configure Worker, HttpClient Factory, and retry policy for HTTP request failures
            services.AddHttpClient<Worker>()
                .AddPolicyHandler((serviceProvider, request) =>
                {
                    //var settings = serviceProvider.GetRequiredService<IOptions<CrawlingSettings>>().Value;
                    var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger>();

                    return HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                        .Or<OperationCanceledException>()
                        .Or<TaskCanceledException>()
                        .WaitAndRetryAsync(
                            settings.HttpErrorRetry,
                            retryAttempt => TimeSpan.FromSeconds(settings.HttpErrorRetrySleep),
                            (response, timespan, retryCount, context) =>
                            {
                                logger.LogError("Request failed in #{0} try: {1}. {2}", retryCount, request.RequestUri, response.Result?.ReasonPhrase ?? response.Exception.Message);
                            });
                });

            // configure logger
            services.AddSingleton<Microsoft.Extensions.Logging.ILogger>(serviceProvider =>
            {
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(config)
                    .CreateLogger();

                return LoggerFactory.Create(builder =>
                {
                    builder.AddSerilog();
                })
                .CreateLogger<Worker>();
            });
        }

        static void Translate()
        {
            // 原文
            string q = "The three-dimensional structures of chromosomes are increasingly being recognized as playing a major role in cellular regulatory states. The efficiency and promiscuity of phage Mu transposition was exploited to directly measure in vivo interactions between genomic loci in E. coli. Two global organizing principles have emerged: first, the chromosome is well-mixed and uncompartmentalized, with transpositions occurring freely between all measured loci; second, several gene families/regions show “clustering”: strong three-dimensional co-localization regardless of linear genomic distance. The activities of the SMC/condensin protein MukB and nucleoid-compacting protein subunit HU-α are essential for the well-mixed state; HU-α is also needed for clustering of 6/7 ribosomal RNA-encoding loci. The data are explained by a model in which the chromosomal structure is driven by dynamic competition between DNA replication and chromosomal relaxation, providing a foundation for determining how region-specific properties contribute to both chromosomal structure and gene regulation.";
            // 源语言
            string from = "en";
            // 目标语言
            string to = "zh";
            // 改成您的APP ID
            string appId = "20200220000386370";
            Random rd = new Random();
            string salt = rd.Next(100000).ToString();
            // 改成您的密钥
            string secretKey = "BFBYCHmQtWV011qK9aRE";
            string sign = EncryptString(appId + q + salt + secretKey);
            string url = "https://fanyi-api.baidu.com/api/trans/vip/translate?";
            url += "q=" + HttpUtility.UrlEncode(q);
            url += "&from=" + from;
            url += "&to=" + to;
            url += "&appid=" + appId;
            url += "&salt=" + salt;
            url += "&sign=" + sign;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.UserAgent = null;
            request.Timeout = 6000;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            Console.WriteLine(retString);
            Console.ReadLine();
        }

        // 计算MD5值
        public static string EncryptString(string str)
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
