using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Serilog;
using System.Net;
using WebCrawler.Common;
using WebCrawler.Crawlers;
using WebCrawler.DataLayer;
using WebCrawler.Models;

/* 
 * TODO References:
 * https://github.com/efcore/EFCore.NamingConventions
 * https://docs.microsoft.com/en-us/ef/core/modeling/value-conversions
*/

namespace WebCrawler.Analyzers
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            AppTools.ConfigureEnvironment();

            var services = new ServiceCollection();

            ConfigureServices(services);

            // NOTICE: Couldn't dispose the service provider here, otherwise it might suffer the issue below, as the Show method will complete immediately.
            // https://github.com/aspnet/DependencyInjection/issues/440#issuecomment-236862811
            using (var serviceProvider = services.BuildServiceProvider())
            {
                //await using (var scope = serviceProvider.CreateAsyncScope())
                {
                    var crawler = serviceProvider.GetRequiredService<ICrawler>();// scope.ServiceProvider.GetRequiredService<ICrawler>();

                    await crawler.ExecuteAsync();
                }
            }
        }

        #region Events

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception);
        }

        private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        #endregion

        #region Private Members

        private static void ConfigureServices(IServiceCollection services)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            services.AddSingleton<IConfiguration>(config);

            services.AddSingleton<CrawlSettings>((serviceProvider) =>
            {
                var crawlSettings = new CrawlSettings();
                config.Bind("Crawling", crawlSettings);

                return crawlSettings;
            });

            // register as Transient, because efcore dbcontext isn't thread safe
            // https://docs.microsoft.com/en-us/ef/core/miscellaneous/configuring-dbcontext#avoiding-dbcontext-threading-issues
            services.AddTransient<IDataLayer, PostgreSQLDataLayer>();
            // configure db context
            services.AddDbContext<ArticleDbContext>(
                options => options.UseNpgsql(
                    config["ConnectionStrings:SqlConnection"],
                    // TODO: https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime
                    builder =>
                    {
                        builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(3), null);
                    }
                ),
                ServiceLifetime.Transient,
                ServiceLifetime.Transient);

            services.AddScoped<ICrawler, ArticleCrawler>();

            // configure Worker, HttpClient Factory, and retry policy for HTTP request failures
            // https://github.com/dotnet/runtime/issues/30025
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.132 Safari/537.36";
            services.AddHttpClient(Constants.HTTP_CLIENT_NAME_DEFAULT, (httpClient) => httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent))
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                })
                .AddPolicyHandler(HttpPolicyHandler);
            services.AddHttpClient(Constants.HTTP_CLIENT_NAME_NOREDIRECT, (httpClient) => httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent))
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                })
                .AddPolicyHandler(HttpPolicyHandler);

            // configure logger
            services.AddLogging(builder =>
            {
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(config)
                    .CreateLogger();

                builder.ClearProviders()
                    .AddSystemdConsole()
                    .AddConsole()
                    .AddSerilog()
                    .AddFilter(lvl => lvl > LogLevel.Information);
            });
        }

        private static AsyncRetryPolicy<HttpResponseMessage> HttpPolicyHandler(IServiceProvider serviceProvider, HttpRequestMessage request)
        {
            IConfiguration config = serviceProvider.GetRequiredService<IConfiguration>();

            //var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger>();

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
                        //logger.LogError("Request failed in #{0} try: {1}. {2}", retryCount, request.RequestUri, response.Result?.ReasonPhrase ?? response.Exception.Message);
                    });
        }

        private static void HandleException(Exception? exception)
        {
            if (exception == null)
            {
                return;
            }

            if (exception.InnerException is ThreadAbortException)
            {
            }
            //else if (exception.GetBaseException() is System.Data.Entity.Validation.DbEntityValidationException valEx)
            //{
            //    StringBuilder builder = new StringBuilder();
            //    foreach (var eve in valEx.EntityValidationErrors)
            //    {
            //        foreach (var ve in eve.ValidationErrors)
            //        {
            //            builder.AppendLine($"{ve.PropertyName}: {ve.ErrorMessage}");
            //        }
            //    }

            //    MessageBox.Show(builder.ToString(), "Validation Errors", MessageBoxButton.OK, MessageBoxImage.Error);
            //}
            else
            {
                exception = (exception is System.Reflection.TargetInvocationException && exception.InnerException != null) ? exception.InnerException : exception;

                Console.WriteLine(exception.ToString());
            }
        }

        #endregion
    }
}
