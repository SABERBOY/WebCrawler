using Cloudtoid.Interprocess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WebCrawler.Proxy.Windows;
using WebCrawler.Queue;

namespace WebCrawler.Proxy
{
    public partial class App : Application
    {
        private Mutex _mutex;

        public App()
        {
            // signle instance application
            _mutex = new Mutex(true, "WebCrawler.Proxy", out bool isNew);

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

            // add encoding support for GB2312 and GDK
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var services = new ServiceCollection();

            ConfigureServices(services);

            // NOTICE: Couldn't dispose the service provider here, otherwise it might suffer the issue below, as the Show method will complete immediately.
            // https://github.com/aspnet/DependencyInjection/issues/440#issuecomment-236862811
            var serviceProvider = services.BuildServiceProvider();

            var window = serviceProvider.GetService<RequestProxy>();

            window.Show();
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
            services.AddSingleton<ProxySettings>((serviceProvider) =>
            {
                var proxySettings = new ProxySettings();
                config.Bind("Proxy", proxySettings);

                return proxySettings;
            });

            services.AddSingleton<MainWindow>();
            services.AddSingleton<RequestProxy>();
            services.AddSingleton<IProxyDispatcher, ProxyDispatcher>();

            // enable cross-platform shared memory queue for fast communication between processes
            services.AddInterprocessQueue();

            // configure logger
            services.AddLogging(builder =>
            {
                Log.Logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(config)
                   .CreateLogger();

                builder.ClearProviders()
                    .AddSerilog()
                    .AddFilter(lvl => lvl > LogLevel.Information);
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
