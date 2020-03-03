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

        private void SiteConfig_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as SiteConfigViewModel;

            vm.LoadData();
        }

        /// <summary>
        /// That's better to not to use async for this event, otherwise, the table might be sorted twice, as the call e.Handled = true might be later than the built-in behavior.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var vm = DataContext as SiteConfigViewModel;

            var direction = e.Column.SortDirection == null || e.Column.SortDirection == ListSortDirection.Descending
                ? ListSortDirection.Ascending
                : ListSortDirection.Descending;

            var sortAccepted = vm.Sort(new SortDescription(e.Column.SortMemberPath, direction));
            if (sortAccepted)
            {
                var grid = sender as DataGrid;

                // update the sort arrow icon status as the sorting is cancelled by e.Handled = true
                foreach (var col in grid.Columns)
                {
                    if (col.SortMemberPath == e.Column.SortMemberPath)
                    {
                        col.SortDirection = direction;
                    }
                    else
                    {
                        col.SortDirection = null;
                    }
                }
            }
        }
    }
}
