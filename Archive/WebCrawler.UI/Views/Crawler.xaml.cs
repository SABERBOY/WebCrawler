using System.Windows.Controls;
using WebCrawler.UI.Common;
using WebCrawler.UI.ViewModels;

namespace WebCrawler.UI.Views
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
