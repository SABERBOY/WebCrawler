using Cloudtoid.Interprocess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Serilog;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WebCrawler.Common;
using WebCrawler.Crawlers;
using WebCrawler.DataLayer;
using WebCrawler.Models;
using WebCrawler.Queue;
using WebCrawler.WPF.Common;
using WebCrawler.WPF.ViewModels;
using WebCrawler.WPF.Views;

namespace WebCrawler.WPF
{
    public partial class App : Application
    {
        private Mutex _mutex;

        public App()
        {
            // signle instance application
            _mutex = new Mutex(true, "WebCrawler.WPF", out bool isNew);

            if (!isNew)
            {
                Environment.Exit(0);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += Application_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            AppTools.ConfigureEnvironment();

            BrowserEmulation.EnableBrowserEmulation();

            var services = new ServiceCollection();

            ConfigureServices(services);

            // NOTICE: Couldn't dispose the service provider here, otherwise it might suffer the issue below, as the Show method will complete immediately.
            // https://github.com/aspnet/DependencyInjection/issues/440#issuecomment-236862811
            var serviceProvider = services.BuildServiceProvider();

            var window = serviceProvider.GetService<MainWindow>();
            Navigator.Initialize(serviceProvider, window.MainFrame.NavigationService);

            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var processes = Process.GetProcessesByName("WebCrawler.Proxy");
            if (processes.Length > 0)
            {
                processes.ForEach(o => o.Kill());
            }

            base.OnExit(e);
        }

        #region Events

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        #endregion

        #region Private Members
        private void ConfigureServices(IServiceCollection services)
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
            services.AddSingleton<ProxySettings>((serviceProvider) =>
            {
                var proxySettings = new ProxySettings();
                config.Bind("Proxy", proxySettings);

                return proxySettings;
            });

            // register as Transient, because efcore dbcontext isn't thread safe
            // https://docs.microsoft.com/en-us/ef/core/miscellaneous/configuring-dbcontext#avoiding-dbcontext-threading-issues
            services.AddTransient<IDataLayer, MySQLDataLayer>();
            // configure db context
            services.AddDbContext<ArticleDbContext>(
                options => options.UseMySql(
                    config["ConnectionStrings:MySQLConnection"],
                    ServerVersion.AutoDetect(config["ConnectionStrings:MySQLConnection"]),
                    builder => builder.EnableRetryOnFailure(3)
                ),
                ServiceLifetime.Transient,
                ServiceLifetime.Transient);

            /*
            //services.AddTransient<IDataLayer, PostgreSQLDataLayer>();
            // configure db context
            services.AddDbContext<ArticleDbContext>(
                options => options.UseNpgsql(
                    config["ConnectionStrings:PostgreSQLConnection"],
                    // TODO: https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime
                    builder =>
                    {
                        builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(3), null);
                    }
                ),
                ServiceLifetime.Transient,
                ServiceLifetime.Transient);
            */

            services.AddSingleton<ICrawler, ArticleCrawler>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<Crawler>();
            services.AddSingleton<Manage>();
            services.AddSingleton<Settings>();
            services.AddSingleton<IProxyDispatcher, ProxyDispatcher>();

            services.AddSingleton<CrawlerViewModel>();
            services.AddSingleton<ManageViewModel>();

            // configure Worker, HttpClient Factory, and retry policy for HTTP request failures
            // https://github.com/dotnet/runtime/issues/30025
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.132 Safari/537.36";
            services.AddHttpClient(
                    Constants.HTTP_CLIENT_NAME_DEFAULT,
                    (httpClient) =>
                    {
                        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

                        var crawlSettings = services.BuildServiceProvider().GetService<CrawlSettings>();
                        if (crawlSettings.HttpClientTimeout > 0)
                        {
                            httpClient.Timeout = TimeSpan.FromSeconds(crawlSettings.HttpClientTimeout);
                        }
                    }
                )
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                })
                .AddPolicyHandler(HttpPolicyHandler);
            services.AddHttpClient(
                    Constants.HTTP_CLIENT_NAME_NOREDIRECT,
                    (httpClient) =>
                    {
                        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

                        var crawlSettings = services.BuildServiceProvider().GetService<CrawlSettings>();
                        if (crawlSettings.HttpClientTimeout > 0)
                        {
                            httpClient.Timeout = TimeSpan.FromSeconds(crawlSettings.HttpClientTimeout);
                        }
                    }
                )
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                })
                .AddPolicyHandler(HttpPolicyHandler);

            // enable cross-platform shared memory queue for fast communication between processes
            services.AddInterprocessQueue();

            // configure logger
            services.AddLogging(builder =>
            {
                Log.Logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(config)
                   .CreateLogger();

                builder.ClearProviders()
                    .AddSerilog()/*
                    .AddFilter(lvl => lvl > LogLevel.Information)*/;
            });
        }

        private AsyncRetryPolicy<HttpResponseMessage> HttpPolicyHandler(IServiceProvider serviceProvider, HttpRequestMessage request)
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

        private void HandleException(Exception exception)
        {
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

                MessageBox.Show(exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
