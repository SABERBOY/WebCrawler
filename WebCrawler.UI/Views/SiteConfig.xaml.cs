using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using WebCrawler.UI.ViewModels;

namespace WebCrawler.UI.Views
{
    public partial class SiteConfig : Page
    {
        public SiteConfig(SiteConfigViewModel configViewModel)
        {
            InitializeComponent();

            DataContext = configViewModel;

            Loaded += SiteConfig_Loaded;
        }

        private async void SiteConfig_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as SiteConfigViewModel;

            await vm.LoadDataAsync();
        }

        private async void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var vm = DataContext as SiteConfigViewModel;

            var direction = e.Column.SortDirection == null || e.Column.SortDirection == ListSortDirection.Descending
                ? ListSortDirection.Ascending
                : ListSortDirection.Descending;

            await vm.Sort(new SortDescription(e.Column.SortMemberPath, direction));

            // TODO: 考虑使用handler event去取消built-in排序功能，然后自己设置column header的排序箭头
            e.Handled = true;
        }
    }
}
