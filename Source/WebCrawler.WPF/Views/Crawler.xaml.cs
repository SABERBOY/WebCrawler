using System.Windows.Controls;
using WebCrawler.WPF.Common;
using WebCrawler.WPF.ViewModels;

namespace WebCrawler.WPF.Views
{
    public partial class Crawler : Page
    {
        private bool _defaultViewDataReady = false;

        public Crawler(CrawlerViewModel crawlerViewModel)
        {
            InitializeComponent();

            DataContext = crawlerViewModel;

            Navigator.NavigationService.LoadCompleted += NavigationService_LoadCompleted;
        }

        private void NavigationService_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.Content != this)
            {
                return;
            }

            if (!_defaultViewDataReady)
            {
                (DataContext as CrawlerViewModel).LoadData();

                _defaultViewDataReady = true;
            }
        }
    }
}
