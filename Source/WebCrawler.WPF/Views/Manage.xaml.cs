using Microsoft.Web.WebView2.Core;
using Serilog;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using WebCrawler.Common;
using WebCrawler.DTO;
using WebCrawler.WPF.Common;
using WebCrawler.WPF.ViewModels;

namespace WebCrawler.WPF.Views
{
    public partial class Manage : Page
    {
        private bool _defaultViewDataReady = false;

        public Manage(ManageViewModel manageViewModel)
        {
            InitializeComponent();

            DataContext = manageViewModel;

            Navigator.NavigationService.LoadCompleted += NavigationService_LoadCompleted;

            Loaded += Manage_Loaded;
        }

        private async void Manage_Loaded(object sender, RoutedEventArgs e)
        {
            // WebView2 controls that share the same user data folder must use the same options, otherwise, the WebView2 creation will fail with 0x8007139F HRESULT_FROM_WIN32(ERROR_INVALID_STATE).
            // https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/userdatafolder#share-user-data-folders
            var userDataFolder = Path.Combine(WCUtility.GetAppTempFolder(), nameof(Manage));

            // Create the environment manually
            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            await webView.EnsureCoreWebView2Async(env);

            webView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed; ;
        }

        private void CoreWebView2_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
        {
            Log.Logger.Error($"WebView2 crashed as {e.Reason}");
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

            vm.AcceptSelectedItems(grid.SelectedItems.Cast<WebsiteDTO>().ToArray());
        }
    }
}
