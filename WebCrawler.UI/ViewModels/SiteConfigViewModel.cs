using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using WebCrawler.UI.Models;
using WebCrawler.UI.Persisters;
using System.Linq;
using WebCrawler.Common;
using System.Net.Http;
using HtmlAgilityPack;

namespace WebCrawler.UI.ViewModels
{
    public class SiteConfigViewModel : NotifyPropertyChanged
    {
        private IPersister _persister;
        private HttpClient _httpClient;

        private SortDescription[] _sorts;

        private CollectionViewSource _websitesSource;
        public ICollectionView WebsitesView
        {
            get
            {
                return _websitesSource.View;
            }
        }

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

                LoadDataAsync().ConfigureAwait(false);
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

                LoadDataAsync().ConfigureAwait(false);
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
            CatalogItems = new ObservableCollection<CatalogItem>();

            _websitesSource = new CollectionViewSource { Source = Websites };

            Editor = new WebsiteEditor();
        }

        public async Task Sort(params SortDescription[] sorts)
        {
            if (!AcquireLock())
            {
                return;
            }

            try
            {
                _sorts = sorts;

                await LoadDataAsync(true);
            }
            catch (System.Exception)
            {
                throw;
            }
            finally
            {
                ReleaseLock();
            }
        }

        public async Task LoadDataAsync(bool skipLock = false)
        {
            if (!skipLock && !AcquireLock())
            {
                return;
            }

            try
            {
                var sort = _sorts?.FirstOrDefault();

                var websites = await _persister.GetWebsitesAsync(Keywords, Enabled, 1, sort?.PropertyName, sort?.Direction == ListSortDirection.Descending);

                Websites.Clear();

                foreach (var web in websites.Items)
                {
                    Websites.Add(web);
                }

                WebsitesView.Refresh();
            }
            catch (System.Exception ex)
            {
                throw;
            }
            finally
            {
                if (!skipLock)
                {
                    ReleaseLock();
                }
            }
        }

        #region Private Members

        private void OnSelectedWebsiteChanged()
        {
            if (SelectedWebsite == null)
            {
                ShowTestResult = false;
            }
            else
            {
                Editor.From(_selectedWebsite);

                CatalogItems.Clear();
            }
        }

        private async void SaveAsync()
        {
            if (!AcquireLock())
            {
                return;
            }

            // TODO

            ReleaseLock();
        }

        private async void RunTestAsync()
        {
            if (!AcquireLock())
            {
                return;
            }

            try
            {
                ShowTestResult = true;

                #region Test catalogs

                CatalogItems.Clear();

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
                        foreach (var item in items)
                        {
                            CatalogItems.Add(item);
                        }
                    }
                }

                #endregion

                // test pagination
                // test details
            }
            catch (System.Exception ex)
            {
                throw;
            }
            finally
            {
                ReleaseLock();
            }
        }

        private void Reset()
        {
            if (!AcquireLock())
            {
                return;
            }

            Editor.From(SelectedWebsite);

            ReleaseLock();
        }

        private async void OnSelectedCatalogItemChanged()
        {
            if (SelectedCatalogItem == null)
            {
                Article = null;

                return;
            }

            if (!AcquireLock())
            {
                return;
            }

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

            ReleaseLock();
        }

        private bool AcquireLock()
        {
            lock (this)
            {
                if (IsProcessing)
                {
                    return false;
                }

                return IsProcessing = true;
            }
        }

        private void ReleaseLock()
        {
            IsProcessing = false;
        }

        #endregion
    }
}
