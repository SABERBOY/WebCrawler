using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace WebCrawler.UI.Common
{
    public static class Navigator
    {
        private static ServiceProvider _serviceProvider;

        public static NavigationService NavigationService { get; private set; }

        public static void Initialize(ServiceProvider serviceProvider, NavigationService service)
        {
            _serviceProvider = serviceProvider;
            NavigationService = service;
        }

        public static void Navigate<T>(object parameter = null)
        {
            //var frame = App.Current.MainWindow.FindDescendants<Frame>().FirstOrDefault();

            //frame?.Navigate(page);

            var page = _serviceProvider.GetRequiredService<T>();

            NavigationService.Navigate(page, parameter);
        }
    }
}
