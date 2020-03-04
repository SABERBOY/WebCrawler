using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;
using Serilog.Events;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using WebCrawler.Common;
using WebCrawler.UI.Models;
using WebCrawler.UI.Persisters;

namespace WebCrawler.UI.ViewModels
{
    public class SiteConfigViewModel : NotifyPropertyChanged
    {
        private IPersister _persister;
        private HttpClient _httpClient;

        private SortDescription[] _sorts;

        #region Notify Properties

        private bool _isProcessing;
        public bool IsProcessing
        {
            get { return _isProcessing; }
            set
            {
                if (_isProcessing == value) { return; }

                _isProcessing = value;
                RaisePropertyChanged();
            }
        }

        private string _keywords;
        public string Keywords
        {
            get { return _keywords; }
            set
            {
                if (_keywords == value) { return; }

                _keywords = value;
                RaisePropertyChanged();

                LoadData();
            }
        }

        private bool _enabled;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled == value) { return; }

                _enabled = value;
                RaisePropertyChanged();

                LoadData();
            }
        }

        private Website _selectedWebsite;
        public Website SelectedWebsite
        {
            get { return _selectedWebsite; }
            set
            {
                if (_selectedWebsite == value) { return; }

                _selectedWebsite = value;
                RaisePropertyChanged();

                OnSelectedWebsiteChanged();
            }
        }

        private WebsiteEditor _editor;
        public WebsiteEditor Editor
        {
            get { return _editor; }
            set
            {
                if (_editor == value) { return; }

                _editor = value;
                RaisePropertyChanged();
            }
        }

        private bool _showTestResult;
        public bool ShowTestResult
        {
            get { return _showTestResult; }
            set
            {
                if (_showTestResult == value) { return; }

                _showTestResult = value;
                RaisePropertyChanged();
            }
        }

        private CatalogItem _selectedCatalogItem;
        public CatalogItem SelectedCatalogItem
        {
            get { return _selectedCatalogItem; }
            set
            {
                if (_selectedCatalogItem == value) { return; }

                _selectedCatalogItem = value;
                RaisePropertyChanged();

                OnSelectedCatalogItemChanged();
            }
        }

        private Article _article;
        public Article Article
        {
            get { return _article; }
            set
            {
                if (_article == value) { return; }

                _article = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<Website> _websites;
        public ObservableCollection<Website> Websites
        {
            get { return _websites; }
            set
            {
                if (_websites == value) { return; }

                _websites = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<CatalogItem> _catalogItems;
        public ObservableCollection<CatalogItem> CatalogItems
        {
            get { return _catalogItems; }
            set
            {
                if (_catalogItems == value) { return; }

                _catalogItems = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<Output> _outputs;
        public ObservableCollection<Output> Outputs
        {
            get { return _outputs; }
            set
            {
                if (_outputs == value) { return; }

                _outputs = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Commands

        private RelayCommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new RelayCommand(SaveAsync, () => SelectedWebsite != null && !IsProcessing);
                }
                return _saveCommand;
            }
        }

        private RelayCommand _testCommand;
        public ICommand TestCommand
        {
            get
            {
                if (_testCommand == null)
                {
                    _testCommand = new RelayCommand(RunTestAsync, () => SelectedWebsite != null && !IsProcessing);
                }
                return _testCommand;
            }
        }

        private RelayCommand _resetCommand;
        public ICommand ResetCommand
        {
            get
            {
                if (_resetCommand == null)
                {
                    _resetCommand = new RelayCommand(Reset, () => SelectedWebsite != null && !IsProcessing);
                }
                return _resetCommand;
            }
        }

        #endregion

        public SiteConfigViewModel(IPersister persister, IHttpClientFactory clientFactory)
        {
            _persister = persister;
            _httpClient = clientFactory.CreateClient(Constants.HTTP_CLIENT_NAME_NOREDIRECT);

            // set the priviate variable to avoid trigger data loading
            _enabled = true;

            Websites = new ObservableCollection<Website>();
            Outputs = new ObservableCollection<Output>();

            Editor = new WebsiteEditor();
        }

        public bool Sort(params SortDescription[] sorts)
        {
            return TryRunAsync(async() =>
            {
                await LoadDataCoreAsync(sorts);
            });
        }

        public void LoadData()
        {
            TryRunAsync(async () =>
            {
                await LoadDataCoreAsync();
            });
        }

        #region Private Members

        private async Task LoadDataCoreAsync(SortDescription[] sorts = null)
        {
            if (sorts != null)
            {
                _sorts = sorts;
            }

            var sort = _sorts?.FirstOrDefault();

            var websites = await _persister.GetWebsitesAsync(Keywords, Enabled, 1, sort?.PropertyName, sort?.Direction == ListSortDirection.Descending);

            App.Current.Dispatcher.Invoke(() =>
            {
                // couldn't create new Websites instance (which don't require Dispatcher Invoke) here as we want to persist the sort arrows in the data grid
                Websites.Clear();
                foreach (var web in websites.Items)
                {
                    Websites.Add(web);
                }
            });
        }

        private void OnSelectedWebsiteChanged()
        {
            if (SelectedWebsite == null)
            {
                ShowTestResult = false;
            }
            else
            {
                Editor.From(_selectedWebsite);

                CatalogItems = null;
            }
        }

        private void SaveAsync()
        {
            TryRunAsync(async () =>
            {
                // TODO
            });
        }

        private void RunTestAsync()
        {
            TryRunAsync(async () =>
            {
                ShowTestResult = true;

                #region Test catalogs

                var data = await _httpClient.GetHtmlAsync(Editor.Home);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(data);

                var listPath = Editor.ListPath;
                if (string.IsNullOrEmpty(listPath))
                {
                    var blocks = HtmlAnalyzer.EvaluateCatalogs(htmlDoc);

                    if (blocks.Length > 0)
                    {
                        var items = HtmlAnalyzer.ExtractCatalogItems(htmlDoc, blocks[0]);

                        CatalogItems = new ObservableCollection<CatalogItem>(items);
                    }
                }

                #endregion

                // test pagination
                // test details
            });
        }

        private void Reset()
        {
            Editor.From(SelectedWebsite);
        }

        private void OnSelectedCatalogItemChanged()
        {
            if (SelectedCatalogItem == null)
            {
                Article = null;

                return;
            }

            TryRunAsync(async () =>
            {
                var url = Utilities.ResolveResourceUrl(SelectedCatalogItem.Url, SelectedWebsite.Home);

                var data = await _httpClient.GetHtmlAsync(url);

                var article = StanSoft.Html2Article.GetArticle(data);

                Article = new Article
                {
                    Url = url,
                    Title = article.Title,
                    Content = article.Content,
                    Published = article.PublishDate
                };
            });
        }

        /// <summary>
        /// Returns true if the call is accepted.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private bool TryRunAsync(Func<Task> action)
        {
            lock (this)
            {
                if (IsProcessing)
                {
                    return false;
                }

                IsProcessing = true;
            }

            Task.Run(async () =>
            {
                try
                {
                    await action?.Invoke();
                }
                catch (Exception ex)
                {
                    AppendOutput(ex.Message, LogEventLevel.Error);
                }
                finally
                {
                    IsProcessing = false;
                }
            });

            return true;
        }

        private void AppendOutput(string message, LogEventLevel level = LogEventLevel.Information)
        {
            lock (this)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Outputs.Insert(0, new Output
                    {
                        Level = level,
                        Message = message,
                        Timestamp = DateTime.Now
                    });

                    while (Outputs.Count > 500)
                    {
                        Outputs.RemoveAt(_outputs.Count - 1);
                    }
                });
            }
        }

        #endregion
    }
}
