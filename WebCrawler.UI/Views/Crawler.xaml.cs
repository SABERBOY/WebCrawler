using System.Windows;
using System.Windows.Controls;
using WebCrawler.UI.ViewModels;

namespace WebCrawler.UI.Views
{
    public partial class Crawler : Page
    {
        private bool _initialized = false;

        public Crawler(CrawlerViewModel crawlerViewModel)
        {
            InitializeComponent();

            DataContext = crawlerViewModel;

            Loaded += Crawler_Loaded;
        }

        private void Crawler_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_initialized)
            {
                (DataContext as CrawlerViewModel).LoadData();
            }

            _initialized = true;
        }
    }
}
