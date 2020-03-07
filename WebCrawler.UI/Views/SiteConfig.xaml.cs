using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WebCrawler.UI.Models;
using WebCrawler.UI.ViewModels;

namespace WebCrawler.UI.Views
{
    public partial class SiteConfig : Page
    {
        public SiteConfig(ManageViewModel configViewModel)
        {
            InitializeComponent();

            DataContext = configViewModel;

            Loaded += SiteConfig_Loaded;
        }

        private void SiteConfig_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ManageViewModel;

            vm.LoadData();
        }

        /// <summary>
        /// That's better to not to use async for this event, otherwise, the table might be sorted twice, as the call e.Handled = true might be later than the built-in behavior.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebsiteGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var vm = DataContext as ManageViewModel;

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

        private void WebsiteGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = DataContext as ManageViewModel;

            var grid = sender as DataGrid;

            vm.AcceptSelectedItems(grid.SelectedItems.Cast<Website>().ToArray());
        }
    }
}
