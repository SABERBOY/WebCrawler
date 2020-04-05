using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Input;
using WebCrawler.Core;
using WebCrawler.Core.Analyzers;
using WebCrawler.UI.Crawlers;
using WebCrawler.UI.Models;
using WebCrawler.UI.Persisters;

namespace WebCrawler.UI.ViewModels
{
    public class ManageViewModel : NotifyPropertyChanged
    {
        private IServiceProvider _serviceProvider;
        private IPersister _persister;
        private ILogger _logger;
        private HttpClient _httpClient;
        private CrawlingSettings _crawlingSettings;

        private SortDescription[] _websiteSorts;
        private WebsiteView[] _websiteSelections;

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

        private string _processingStatus;
        public string ProcessingStatus
        {
            get { return _processingStatus; }
            set
            {
                if (_processingStatus == value) { return; }

                _processingStatus = value;
                RaisePropertyChanged();
            }
        }

        private string _keywordsFilter;
        public string KeywordsFilter
        {
            get { return _keywordsFilter; }
            set
            {
                if (_keywordsFilter == value) { return; }

                _keywordsFilter = value;
                RaisePropertyChanged();

                LoadData();
            }
        }

        private WebsiteStatus _statusFilter;
        public WebsiteStatus StatusFilter
        {
            get { return _statusFilter; }
            set
            {
                if (_statusFilter == value) { return; }

                _statusFilter = value;
                RaisePropertyChanged();

                LoadData();
            }
        }

        private bool? _enabledFilter;
        public bool? EnabledFilter
        {
            get { return _enabledFilter; }
            set
            {
                if (_enabledFilter == value) { return; }

                _enabledFilter = value;
                RaisePropertyChanged();

                LoadData();
            }
        }

        private WebsiteView _selectedWebsite;
        public WebsiteView SelectedWebsite
        {
            get { return _selectedWebsite; }
            set
            {
                if (_selectedWebsite == value) { return; }

                _selectedWebsite = value;
                RaisePropertyChanged();

                // TODO: This additional async call will cause the buttons state not refreshed
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

        private Output _selectedOutput;
        public Output SelectedOutput
        {
            get { return _selectedOutput; }
            set
            {
                if (_selectedOutput == value) { return; }

                _selectedOutput = value;
                RaisePropertyChanged();
            }
        }

        private int _selectedViewIndex;
        public int SelectedViewIndex
        {
            get { return _selectedViewIndex; }
            set
            {
                if (_selectedViewIndex == value) { return; }

                _selectedViewIndex = value;
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

        private bool? _toggleAsEnabled;
        public bool? ToggleAsEnable
        {
            get { return _toggleAsEnabled; }
            set
            {
                if (_toggleAsEnabled == value) { return; }

                _toggleAsEnabled = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<WebsiteView> _websites;
        /// <summary>
        /// Couldn't always create new Websites instance (which don't require Dispatcher Invoke) here as we want to persist the sort arrows in the data grid
        /// </summary>
        public ObservableCollection<WebsiteView> Websites
        {
            get { return _websites; }
            set
            {
                if (_websites == value) { return; }

                _websites = value;
                RaisePropertyChanged();
            }
        }

        private PageInfo _pageInfo;
        public PageInfo PageInfo
        {
            get { return _pageInfo; }
            set
            {
                if (_pageInfo == value) { return; }

                _pageInfo = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<CrawlLog> _crawlLogs;
        public ObservableCollection<CrawlLog> CrawlLogs
        {
            get { return _crawlLogs; }
            set
            {
                if (_crawlLogs == value) { return; }

                _crawlLogs = value;
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

        private RelayCommand _backCommand;
        public ICommand BackCommand
        {
            get
            {
                if (_backCommand == null)
                {
                    _backCommand = new RelayCommand(Back);
                }
                return _backCommand;
            }
        }

        private RelayCommand _refreshCommand;
        public ICommand RefreshCommand
        {
            get
            {
                if (_refreshCommand == null)
                {
                    _refreshCommand = new RelayCommand(() => LoadData(), () => !IsProcessing);
                }
                return _refreshCommand;
            }
        }

        private RelayCommand _analyzeCommand;
        public ICommand AnalyzeCommand
        {
            get
            {
                if (_analyzeCommand == null)
                {
                    _analyzeCommand = new RelayCommand(Analyze, () => !IsProcessing);
                }
                return _analyzeCommand;
            }
        }

        private RelayCommand _navigateCommand;
        public ICommand NavigateCommand
        {
            get
            {
                if (_navigateCommand == null)
                {
                    _navigateCommand = new RelayCommand(Navigate, () => !IsProcessing);
                }
                return _navigateCommand;
            }
        }

        private RelayCommand _toggleSelectedCommand;
        public ICommand ToggleSelectedCommand
        {
            get
            {
                if (_toggleSelectedCommand == null)
                {
                    _toggleSelectedCommand = new RelayCommand(ToggleSelected, () => SelectedWebsite != null && !IsProcessing);
                }
                return _toggleSelectedCommand;
            }
        }

        private RelayCommand _deleteSelectedCommand;
        public ICommand DeleteSelectedCommand
        {
            get
            {
                if (_deleteSelectedCommand == null)
                {
                    _deleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedWebsite != null && !IsProcessing);
                }
                return _deleteSelectedCommand;
            }
        }

        private RelayCommand _addCommand;
        public ICommand AddCommand
        {
            get
            {
                if (_addCommand == null)
                {
                    _addCommand = new RelayCommand(Add, () => !IsProcessing);
                }
                return _addCommand;
            }
        }

        private RelayCommand _saveCurrentCommand;
        public ICommand SaveCurrentCommand
        {
            get
            {
                if (_saveCurrentCommand == null)
                {
                    _saveCurrentCommand = new RelayCommand(Save, () => !IsProcessing);
                }
                return _saveCurrentCommand;
            }
        }

        private RelayCommand _testCurrentCommand;
        public ICommand TestCurrentCommand
        {
            get
            {
                if (_testCurrentCommand == null)
                {
                    _testCurrentCommand = new RelayCommand(RunTest, () => !IsProcessing);
                }
                return _testCurrentCommand;
            }
        }

        private RelayCommand _resetCurrentCommand;
        public ICommand ResetCurrentCommand
        {
            get
            {
                if (_resetCurrentCommand == null)
                {
                    _resetCurrentCommand = new RelayCommand(Reset, () => !IsProcessing);
                }
                return _resetCurrentCommand;
            }
        }

        private RelayCommand _deleteCurrentCommand;
        public ICommand DeleteCurrentCommand
        {
            get
            {
                if (_deleteCurrentCommand == null)
                {
                    _deleteCurrentCommand = new RelayCommand(Delete, () => Editor.Website.Id > 0 && !IsProcessing);
                }
                return _deleteCurrentCommand;
            }
        }

        private RelayCommand _viewOutputWebsiteCommand;
        public ICommand ViewOutputWebsiteCommand
        {
            get
            {
                if (_viewOutputWebsiteCommand == null)
                {
                    _viewOutputWebsiteCommand = new RelayCommand(ViewOutputWebsite, () => SelectedOutput?.WebsiteId != null && !IsProcessing);
                }
                return _viewOutputWebsiteCommand;
            }
        }

        #endregion

        public ManageViewModel(IServiceProvider serviceProvider, IPersister persister, ILogger logger, IHttpClientFactory clientFactory, CrawlingSettings crawlingSettings)
        {
            _serviceProvider = serviceProvider;
            _persister = persister;
            _logger = logger;
            _httpClient = clientFactory.CreateClient(Constants.HTTP_CLIENT_NAME_DEFAULT);
            _crawlingSettings = crawlingSettings;

            Websites = new ObservableCollection<WebsiteView>();
            Outputs = new ObservableCollection<Output>();
            Editor = new WebsiteEditor
            {
                Website = new WebsiteView()
            };
            Editor.PropertyChanged += Editor_PropertyChanged;
            Editor.Website.PropertyChanged += Website_PropertyChanged;
        }

        public bool Sort(params SortDescription[] sorts)
        {
            return TryRunAsync(async () =>
            {
                await LoadDataCoreAsync(sorts);
            });
        }

        public void LoadData(int[] websites = null)
        {
            TryRunAsync(async () =>
            {
                await LoadDataCoreAsync(websiteIds: websites);
            });
        }

        public void AcceptSelectedItems(WebsiteView[] websites)
        {
            _websiteSelections = websites;

            RefreshToggleState();
        }

        public void SearchHtmlNodes(string keywords)
        {
            Link[] links;
            if (string.IsNullOrEmpty(keywords) || Editor.HtmlDoc == null)
            {
                links = new Link[0];
            }
            else
            {
                links = HtmlAnalyzer.GetValidLinks(Editor.HtmlDoc)
                    .Where(o => o.Text.Contains(keywords))
                    .ToArray();
            }

            Editor.NodeSuggestions = new ObservableCollection<Link>(links);
        }

        #region Private Members

        private async Task LoadDataCoreAsync(SortDescription[] sorts = null, int[] websiteIds = null, int page = 1)
        {
            if (sorts != null)
            {
                _websiteSorts = sorts;
            }

            var sort = _websiteSorts?.FirstOrDefault();

            PagedResult<Website> websites;
            if (websiteIds == null || websiteIds.Length == 0)
            {
                websites = await _persister.GetWebsitesAsync(KeywordsFilter, StatusFilter, EnabledFilter, false, page, sort?.PropertyName, sort?.Direction == ListSortDirection.Descending);
            }
            else
            {
                websites = new PagedResult<Website>
                {
                    Items = await _persister.GetWebsitesAsync(websiteIds, false),
                    PageInfo = null
                };
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                // couldn't create new Websites instance (which don't require Dispatcher Invoke) here as we want to persist the sort arrows in the data grid
                Websites.Clear();
                foreach (var web in websites.Items)
                {
                    Websites.Add(new WebsiteView(web));
                }

                PageInfo = websites.PageInfo;
            });
        }

        private void LoadHtml()
        {
            TryRunAsync(async () =>
            {
                await LoadHtmlCoreAsync();
            });
        }

        private void OnSelectedWebsiteChanged()
        {
            CrawlLogs = null;
            CatalogItems = null;

            if (SelectedWebsite == null)
            {
                Editor.IsEditing = false;
            }
            else
            {
                Editor.IsEditing = true;

                SelectedWebsite.CloneTo(Editor.Website);

                TryRunAsync(async () =>
                {
                    var logs = await _persister.GetCrawlLogsAsync(websiteId: SelectedWebsite.Id);
                    CrawlLogs = new ObservableCollection<CrawlLog>(logs.Items);

                    await LoadHtmlCoreAsync();
                });
            }
        }

        private async Task LoadHtmlCoreAsync()
        {
            Editor.Response = null;

            if (!string.IsNullOrEmpty(Editor.Website.Home))
            {
                Editor.Response = await HtmlHelper.GetHtmlAsync(Editor.Website.Home, _httpClient);

                HtmlHelper.HandleParseErrorsIfAny(Editor.HtmlDoc, (errors) => AppendOutput(errors, Editor.Website.Home, Editor.Website.Id, LogEventLevel.Warning));

                if (Editor.Response.IsRedirected)
                {
                    AppendOutput("Url redirected to: " + Editor.Response.ActualUrl, Editor.Website.Home, Editor.Website.Id, LogEventLevel.Warning);
                }

                SelectedViewIndex = 1;
            }
        }

        private void Back()
        {
            NavigationCommands.BrowseBack.Execute(null, null);
        }

        private void Analyze()
        {
            var dialogResult = MessageBox.Show("Start full analysis?", "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (dialogResult == MessageBoxResult.Cancel)
            {
                return;
            }

            var isFull = dialogResult == MessageBoxResult.Yes;

            TryRunAsync(async () =>
            {
                ProcessingStatus = "Processing";

                AppendOutput("Started analysis");

                var start = DateTime.Now;

                int processed = 0;
                int total = 0;

                ActionBlock<Website> workerBlock = null;
                workerBlock = new ActionBlock<Website>(async website =>
                {
                    var result = await TestAsync(website.Id, website.Home, website.ListPath, website.ValidateDate, website.Status, website.SysNotes);

                    try
                    {
                        using (var persister = _serviceProvider.GetRequiredService<IPersister>())
                        {
                            await persister.UpdateStatusAsync(website.Id, result.Status, result.Notes);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendOutput((ex.InnerException ?? ex).Message, website.Home, website.Id, LogEventLevel.Error);

                        _logger.LogError(ex, website.Home);
                    }

                    lock (this)
                    {
                        processed++;
                    }

                    ProcessingStatus = $"Processing {workerBlock.InputCount}/{processed}/{total}";
                }, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _crawlingSettings.MaxDegreeOfParallelism
                });

                PagedResult<Website> websitesQueue = null;
                do
                {
                    websitesQueue = _persister.GetWebsiteAnalysisQueueAsync(isFull, websitesQueue?.Items.Last().Id).Result;

                    if (total == 0)
                    {
                        total = websitesQueue.PageInfo.ItemCount;

                        ProcessingStatus = $"Processing {workerBlock.InputCount}/{processed}/{total}";
                    }

                    foreach (var website in websitesQueue.Items)
                    {
                        workerBlock.Post(website);

                        ProcessingStatus = $"Processing {workerBlock.InputCount}/{processed}/{total}";

                        // accept queue items in the amount of batch size x 3
                        while (workerBlock.InputCount >= _crawlingSettings.MaxDegreeOfParallelism * 2)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                } while (websitesQueue.PageInfo.PageCount > 1);

                workerBlock.Complete();
                workerBlock.Completion.Wait();

                AppendOutput($"Completed analysis, elapsed time: {DateTime.Now - start}");

                // reload data
                await LoadDataCoreAsync();
            });
        }

        private void ToggleSelected()
        {
            if (MessageBox.Show($"Are you sure to {(ToggleAsEnable.Value ? "enable" : "disable")} {_websiteSelections.Length} selected websites?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            TryRunAsync(async () =>
            {
                await _persister.ToggleAsync(ToggleAsEnable.Value, _websiteSelections.Select(o => o.Id).ToArray());

                // refresh the website list, but do not sync the editor as that might not be expected
                _websiteSelections.ForEach(o => o.Enabled = ToggleAsEnable.Value);

                RefreshToggleState();
            });
        }

        private void DeleteSelected()
        {
            if (MessageBox.Show($"Are you sure to delete {_websiteSelections.Length} selected websites?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            TryRunAsync(async () =>
            {
                await _persister.DeleteAsync(_websiteSelections.Select(o => o.Id).ToArray());

                App.Current.Dispatcher.Invoke(() =>
                {
                    _websiteSelections.ForEach(o => Websites.Remove(o));
                });
            });
        }

        private void Add()
        {
            SelectedWebsite = null;
            Editor.IsEditing = true;

            new WebsiteView
            {
                Rank = 1,
                Enabled = true,
                ValidateDate = true,
                Status = WebsiteStatus.Normal
            }.CloneTo(Editor.Website);
        }

        private void Navigate()
        {
            TryRunAsync(async () =>
            {
                await LoadDataCoreAsync(page: PageInfo?.CurrentPage ?? 1);
            });
        }

        private void Save()
        {
            TryRunAsync(async () =>
            {
                var isNew = Editor.Website.Id == 0;

                await _persister.SaveAsync(Editor.Website);

                if (isNew)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var website = Editor.Website.CloneTo();

                        Websites.Insert(0, website);

                        SelectedWebsite = website;
                    });
                }
                else
                {
                    // the current website might not be the selected one, as it might be opened from the output list
                    var website = Websites.FirstOrDefault(o => o.Id == Editor.Website.Id);
                    if (website != null)
                    {
                        Editor.Website.CloneTo(website);
                    }

                    RefreshToggleState();
                }
            });
        }

        private void RunTest()
        {
            TryRunAsync(async () =>
            {
                // test catalogs
                var result = await TestAsync(Editor.Website.Id, Editor.Website.Home, Editor.Website.ListPath, Editor.Website.ValidateDate, Editor.Website.Status, Editor.Website.SysNotes, Editor.Response);
                if (result.Catalogs != null)
                {
                    CatalogItems = new ObservableCollection<CatalogItem>(result.Catalogs);
                }

                // update editor but doens't commit
                if (result.Status != null)
                {
                    Editor.Website.Status = result.Status.Value;
                }
                Editor.Website.SysNotes = result.Notes;

                // TODO: test pagination

                SelectedViewIndex = 0;
            });
        }

        private async Task<TestResult> TestAsync(int websiteId, string url, string listPath, bool validateDate, WebsiteStatus? previousStatus = null, string previousSysNotes = null, ResponseData response = null)
        {
            var result = new TestResult { Status = WebsiteStatus.Normal };

            try
            {
                if (response == null)
                {
                    result.CatalogsResponse = await HtmlHelper.GetHtmlAsync(url, _httpClient);
                }
                else
                {
                    result.CatalogsResponse = response;
                }

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(result.CatalogsResponse.Content);

                // suppress HTML parsing error in TestAsync as there're too many warnings, such error will still be handled in single website mode
                //if (response == null)
                //{
                //    // show parsing errors only when the HTML is requested in this scope
                //    htmlDoc.HandleParseErrorsIfAny((errors) => AppendOutput(errors, url, LogEventLevel.Warning));
                //}

                result.Catalogs = HtmlAnalyzer.DetectCatalogItems(htmlDoc, listPath, validateDate);

                if (result.CatalogsResponse.IsRedirectedExcludeHttps)
                {
                    result.Status = WebsiteStatus.WarningRedirected;
                    result.Notes = "URL redirected to: " + result.CatalogsResponse.ActualUrl;
                }
                else if (result.Catalogs.Length == 0)
                {
                    result.Status = WebsiteStatus.ErrorCatalogMissing;
                    result.Notes = "No catalogs detected";
                }
                else if (validateDate)
                {
                    if (result.Catalogs.Any(o => !o.HasDate))
                    {
                        result.Status = WebsiteStatus.WarningNoDates;
                        result.Notes = "No published date in catalog items";
                    }
                    else
                    {
                        var latestItem = result.Catalogs.OrderByDescending(o => o.Published).FirstOrDefault();
                        if (latestItem.Published != null && latestItem.Published < DateTime.Now.AddDays(_crawlingSettings.OutdateDaysAgo * -1))
                        {
                            result.Status = WebsiteStatus.ErrorOutdate;
                            result.Notes = $"Last published: {latestItem.Published}";
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                var baseEx = ex.GetBaseException();

                result.Status = WebsiteStatus.ErrorBroken;
                result.Notes = $"{baseEx.GetType().Name}: {baseEx.Message}";
            }
            catch (Exception ex)
            {
                // keep previous stats as it might not be a website issue as usual, e.g. TaskCanceledException
                result.Status = null;
                result.Notes = ex.Message;

                _logger.LogError(ex, url);
            }

            // output for changes only
            if (result.Status == null)
            {
                if (!string.IsNullOrEmpty(result.Notes))
                {
                    AppendOutput(result.Notes, url, websiteId, LogEventLevel.Warning);
                }
            }
            else if (result.Status != previousStatus || result.Notes != previousSysNotes)
            {
                var message = string.IsNullOrWhiteSpace(result.Notes) ? $"{result.Status}" : $"{result.Status} | {result.Notes}";
                var level = result.Status == WebsiteStatus.Normal ? LogEventLevel.Information : LogEventLevel.Warning;

                AppendOutput(message, url, websiteId, level);
            }

            return result;
        }

        private void Reset()
        {
            if (Editor.Website.Id == 0)
            {
                Add();
            }
            else
            {
                // the current website might not be the selected one, as it might be opened from the output list
                var website = Websites.FirstOrDefault(o => o.Id == Editor.Website.Id);
                if (website != null)
                {
                    website.CloneTo(Editor.Website);
                }
            }
        }

        private void Delete()
        {
            if (MessageBox.Show($"Are you sure to delete [{Editor.Website.Name}]?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            TryRunAsync(async () =>
            {
                await _persister.DeleteAsync(Editor.Website.Id);

                // the current website might not be the selected one, as it might be opened from the output list
                var website = Websites.FirstOrDefault(o => o.Id == Editor.Website.Id);
                if (website != null)
                {
                    App.Current.Dispatcher.Invoke(() => Websites.Remove(website));
                }
            });
        }

        private void ViewOutputWebsite()
        {
            if (SelectedOutput?.WebsiteId == null)
            {
                return;
            }

            TryRunAsync(async () =>
            {
                SelectedWebsite = null;
                Editor.IsEditing = true;

                var website = await _persister.GetAsync<Website>(SelectedOutput.WebsiteId.Value);

                new WebsiteView(website).CloneTo(Editor.Website);

                // trigger html loading manually as that wouldn't be auto-triggered inside the TryRunAsync context (IsProcessing = true)
                await LoadHtmlCoreAsync();
            });
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
                var data = await HtmlHelper.GetHtmlAsync(SelectedCatalogItem.Url, _httpClient);

                var article = Html2Article.GetArticle(data.Content);

                Article = new Article
                {
                    Url = SelectedCatalogItem.Url,
                    Title = article.Title,
                    Content = article.Content,
                    Published = article.PublishDate
                };
            });
        }

        private void RefreshToggleState()
        {
            if (_websiteSelections == null || _websiteSelections.Length == 0)
            {
                ToggleAsEnable = null;
            }
            else if (_websiteSelections.Any(o => o.Enabled))
            {
                ToggleAsEnable = false;
            }
            else
            {
                ToggleAsEnable = true;
            }
        }

        private void Editor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WebsiteEditor.SelectedNode) && Editor.SelectedNode != null)
            {
                Editor.Website.ListPath = HtmlAnalyzer.DetectListPath(Editor.HtmlDoc, Editor.SelectedNode.XPath);
            }
        }

        private void Website_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WebsiteView.Home))
            {
                LoadHtml();
            }
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
                    AppendOutput((ex.InnerException ?? ex).Message, level: LogEventLevel.Error);

                    _logger.LogError(ex, null);
                }
                finally
                {
                    IsProcessing = false;
                }
            });

            return true;
        }

        private void AppendOutput(string message, string url = null, int? websiteId = null, LogEventLevel level = LogEventLevel.Information)
        {
            lock (this)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Outputs.Insert(0, new Output
                    {
                        Level = level,
                        URL = url,
                        WebsiteId = websiteId,
                        Message = message,
                        Timestamp = DateTime.Now
                    });

                    while (Outputs.Count > Common.Constants.OUTPUTS_MAX)
                    {
                        Outputs.RemoveAt(_outputs.Count - 1);
                    }
                });
            }
        }

        #endregion
    }
}
