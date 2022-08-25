using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using WebCrawler.Common;
using WebCrawler.DataLayer;
using WebCrawler.Models;

/* 
 * TODO References:
 * https://github.com/efcore/EFCore.NamingConventions
 * https://docs.microsoft.com/en-us/ef/core/modeling/value-conversions
*/

namespace WebCrawler.DataMigrator
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
                var migrator = serviceProvider.GetRequiredService<WebsiteRulesMigrator>();// scope.ServiceProvider.GetRequiredService<ICrawler>();

                await migrator.ExecuteAsync();
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

            services.AddDbContext<ArticleDbContextPG>(
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

            services.AddScoped<WebsiteRulesMigrator>();

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
