using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using WebCrawler.Common;
using WebCrawler.UI.Crawlers;
using WebCrawler.UI.Models;
using WebCrawler.UI.Persisters;

namespace WebCrawler.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            ConfigureServices(services);

            using (var serviceProvider = services.BuildServiceProvider())
            {
                serviceProvider.GetService<MainWindow>().Show();
            }
        }

        static void ConfigureServices(IServiceCollection services)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            //services.AddSingleton<IConfiguration>(config);

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
            services.AddDbContext<ArticleDbContext>(
                options => options.UseMySql(config["ConnectionStrings:MySqlConnection"], builder => builder.EnableRetryOnFailure(3)),
                ServiceLifetime.Transient,
                ServiceLifetime.Transient);

            services.AddTransient<MainWindow>();

            // configure Worker, HttpClient Factory, and retry policy for HTTP request failures
            services.AddHttpClient(Constants.HTTP_CLIENT_NAME_DEFAULT)
                .AddPolicyHandler((serviceProvider, request) =>
                {
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
                .CreateLogger<MainWindow>();
            });
        }
    }
}
