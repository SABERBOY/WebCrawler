using System.Windows;
using System.Windows.Controls;
using WebCrawler.UI.ViewModels;

namespace WebCrawler.UI.Views
{
    public partial class Crawler : Page
    {
        public Crawler(CrawlerViewModel crawlerViewModel)
        {
            InitializeComponent();

            DataContext = crawlerViewModel;

            Loaded += Crawler_Loaded;
        }

        private void Crawler_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as CrawlerViewModel;

            vm.LoadData();
        }
    }
}
