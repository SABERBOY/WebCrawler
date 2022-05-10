using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebCrawler.Core;
using WebCrawler.Housing.Crawlers;
using WebCrawler.Housing.Models;
using WebCrawler.Housing.Persisters;

namespace WebCrawler.Housing
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // add encoding support for GB2312 and GDK
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Console.OutputEncoding = Encoding.GetEncoding("GB2312");

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

            services.AddSingleton<CrawlingSettings>((serviceProvider) =>
            {
                var crawlSettings = new CrawlingSettings();
                config.Bind("Crawling", crawlSettings);

                return crawlSettings;
            });

            // register as Transient, because efcore dbcontext isn't thread safe
            // https://docs.microsoft.com/en-us/ef/core/miscellaneous/configuring-dbcontext#avoiding-dbcontext-threading-issues
            services.AddTransient<IPersister, MySqlPersister>();
            // configure db context
            services.AddDbContext<HousingDbContext>(
                options => options.UseMySql(config["ConnectionStrings:MySqlConnection"], builder => builder.EnableRetryOnFailure(3)),
                ServiceLifetime.Transient,
                ServiceLifetime.Transient);
            //services.AddDbContextPool<ArticleDbContext>(options => options.UseMySql(config["ConnectionStrings:MySqlConnection"]));

            services.AddTransient<ICrawler, HousingCrawler>();
            services.AddTransient<Worker>();

            // configure Worker, HttpClient Factory, and retry policy for HTTP request failures
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.132 Safari/537.36";
            services.AddHttpClient(Constants.HTTP_CLIENT_NAME_DEFAULT, (httpClient) => httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent))
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                })
                .AddPolicyHandler(HttpPolicyHandler);

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

        private static AsyncRetryPolicy<HttpResponseMessage> HttpPolicyHandler(IServiceProvider serviceProvider, HttpRequestMessage request)
        {
            IConfiguration config = serviceProvider.GetRequiredService<IConfiguration>();

            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger>();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
                .Or<OperationCanceledException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    int.Parse(config["HttpClient:HttpErrorRetry"]),
                    retryAttempt => TimeSpan.FromSeconds(int.Parse(config["HttpClient:HttpErrorRetrySleep"])),
                    (response, timespan, retryCount, context) =>
                    {
                        logger.LogError("Request failed in #{0} try: {1}. {2}", retryCount, request.RequestUri, response.Result?.ReasonPhrase ?? response.Exception.Message);
                    });
        }
    }
}
