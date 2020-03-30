using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using WebCrawler.UI.Common;
using WebCrawler.UI.ViewModels;

namespace WebCrawler.UI.Views
{
    public partial class Manage : Page
    {
        private bool _defaultViewDataReady = false;

        public Manage(ManageViewModel manageViewModel)
        {
            InitializeComponent();

            DataContext = manageViewModel;

            Navigator.NavigationService.LoadCompleted += NavigationService_LoadCompleted;
        }

        private void NavigationService_LoadCompleted(object sender, NavigationEventArgs e)
        {
            if (e.Content != this)
            {
                return;
            }

            var manageViewModel = DataContext as ManageViewModel;

            if (e.ExtraData is int[] websiteIds)
            {
                manageViewModel.LoadData(websiteIds);

                _defaultViewDataReady = false;
            }
            else
            {
                if (!_defaultViewDataReady)
                {
                    manageViewModel.LoadData();

                    _defaultViewDataReady = true;
                }
            }
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

            vm.AcceptSelectedItems(grid.SelectedItems.Cast<WebsiteView>().ToArray());
        }

        private void Spinner_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var processing = Convert.ToBoolean(e.NewValue);

            webBrowser.Visibility = processing ? Visibility.Hidden : Visibility.Visible;
        }

        private void ListPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var keywords = (sender as TextBox).Text;

            var vm = DataContext as ManageViewModel;

            vm.SearchHtmlNodes(keywords.Trim());
        }

        private void ListPathTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            listBoxSuggestions.Width = (sender as TextBox).ActualWidth;
        }

        private void webBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            webBrowser.SuppressScriptErrors();
        }
    }
}
