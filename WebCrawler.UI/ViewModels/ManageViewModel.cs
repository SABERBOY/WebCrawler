using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
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
using WebCrawler.Common;
using WebCrawler.Common.Analyzers;
using WebCrawler.UI.Crawlers;
using WebCrawler.UI.Models;
using WebCrawler.UI.Persisters;

namespace WebCrawler.UI.ViewModels
{
    public class ManageViewModel : NotifyPropertyChanged
    {
        private IServiceProvider _serviceProvider;
        private IPersister _persister;
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
                    _deleteCurrentCommand = new RelayCommand(Delete, () => SelectedWebsite != null && !IsProcessing);
                }
                return _deleteCurrentCommand;
            }
        }

        #endregion

        public ManageViewModel(IServiceProvider serviceProvider, IPersister persister, IHttpClientFactory clientFactory, CrawlingSettings crawlingSettings)
        {
            _serviceProvider = serviceProvider;
            _persister = persister;
            _httpClient = clientFactory.CreateClient(Constants.HTTP_CLIENT_NAME_DEFAULT);
            _crawlingSettings = crawlingSettings;

            Websites = new ObservableCollection<WebsiteView>();
            Outputs = new ObservableCollection<Output>();
            Editor = new WebsiteEditor();
            Editor.PropertyChanged += Editor_PropertyChanged;
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

        public void LoadHtml()
        {
            TryRunAsync(async () =>
            {
                await LoadHtmlCoreAsync();
            });
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

        private void OnSelectedWebsiteChanged()
        {
            CrawlLogs = null;
            CatalogItems = null;

            if (SelectedWebsite == null)
            {
                Editor.Website = null;
            }
            else
            {
                Editor.Website = SelectedWebsite.Clone();

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
            Editor.Html = null;
            if (!string.IsNullOrEmpty(Editor.Website.Home))
            {
                var data = await _httpClient.GetHtmlAsync(Editor.Website.Home);
                Editor.Html = data.Content;

                Editor.HtmlDoc.HandleParseErrorsIfAny((errors) => AppendOutput(errors, LogEventLevel.Warning));

                if (data.IsRedirected)
                {
                    AppendOutput("Url redirected to: " + data.ActualUrl, LogEventLevel.Warning);
                }
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
                    WebsiteStatus status = WebsiteStatus.Normal;
                    string notes = null;

                    try
                    {
                        var result = await TestAsync(website.Home, website.ListPath);

                        if (result.CatalogsResponse.IsRedirectedExcludeHttps)
                        {
                            status = WebsiteStatus.WarningRedirected;
                            notes = "URL redirected to: " + result.CatalogsResponse.ActualUrl;
                        }
                        else if (result.Catalogs.Length == 0)
                        {
                            status = WebsiteStatus.ErrorCatalogMissing;
                            notes = "No catalogs detected";
                        }
                        else
                        {
                            if (result.Catalogs.Any(o => !o.HasDate))
                            {
                                // check dates only when ListPath isn't provided
                                if (string.IsNullOrEmpty(website.ListPath))
                                {
                                    status = WebsiteStatus.WarningNoDates;
                                    notes = "No published date in catalog items";
                                }
                            }
                            else
                            {
                                var latestItem = result.Catalogs.OrderByDescending(o => o.Published).FirstOrDefault();
                                if (latestItem.Published != null && latestItem.Published < DateTime.Now.AddDays(_crawlingSettings.OutdateDaysAgo * -1))
                                {
                                    status = WebsiteStatus.ErrorOutdate;
                                    notes = $"Last published: {latestItem.Published}";
                                }
                            }
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        var baseEx = ex.GetBaseException();

                        status = WebsiteStatus.ErrorBroken;
                        notes = $"{baseEx.GetType().Name}: {baseEx.Message}";
                    }
                    catch (Exception ex)
                    {
                        // keep previous stats as it might not be a website issue as usual, e.g. TaskCanceledException
                        status = website.Status;
                        notes = ex.Message;
                    }

                    // output for changes only
                    if (status != website.Status || notes != website.SysNotes)
                    {
                        AppendOutput($"{website.Name}: {status}: {notes}", status == WebsiteStatus.Normal ? LogEventLevel.Information : LogEventLevel.Warning);
                    }

                    try
                    {
                        using (var persister = _serviceProvider.GetRequiredService<IPersister>())
                        {
                            await persister.UpdateStatusAsync(website.Id, status, notes);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendOutput($"{(ex.InnerException ?? ex).Message} {website.Home}", LogEventLevel.Error);
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

            Editor.Website = new WebsiteView
            {
                Rank = 1,
                Enabled = true,
                Status = WebsiteStatus.Normal
            };
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
                        var website = Editor.Website.Clone();

                        Websites.Insert(0, website);

                        SelectedWebsite = website;
                    });
                }
                else
                {
                    Editor.Website.Clone(SelectedWebsite);

                    RefreshToggleState();
                }
            });
        }

        private void RunTest()
        {
            TryRunAsync(async () =>
            {
                // test catalogs
                var result = await TestAsync(Editor.Website.Home, Editor.Website.ListPath, Editor.HtmlDoc);
                CatalogItems = new ObservableCollection<CatalogItem>(result.Catalogs);

                // TODO: test pagination
            });
        }

        private async Task<TestResult> TestAsync(string url = null, string listPath = null, HtmlDocument htmlDoc = null)
        {
            var result = new TestResult();

            if (htmlDoc == null)
            {
                var data = await _httpClient.GetHtmlAsync(url);

                result.CatalogsResponse = data;

                htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(data.Content);

                htmlDoc.HandleParseErrorsIfAny((errors) => AppendOutput(errors, LogEventLevel.Warning));
            }

            result.Catalogs = HtmlAnalyzer.ExtractCatalogItems(htmlDoc, listPath);

            return result;
        }

        private void Reset()
        {
            if (SelectedWebsite == null)
            {
                Add();
            }
            else
            {
                SelectedWebsite.Clone(Editor.Website);
            }
        }

        private void Delete()
        {
            if (MessageBox.Show($"Are you sure to delete [{SelectedWebsite.Name}]?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            TryRunAsync(async () =>
            {
                await _persister.DeleteAsync(SelectedWebsite.Id);

                App.Current.Dispatcher.Invoke(() => Websites.Remove(SelectedWebsite));
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
                var data = await _httpClient.GetHtmlAsync(SelectedCatalogItem.Url);

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
                var links = HtmlAnalyzer.GetValidLinks(Editor.HtmlDoc);

                Editor.Website.ListPath = HtmlAnalyzer.GetListPath(links, Editor.SelectedNode.XPath);
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
                    AppendOutput((ex.InnerException ?? ex).Message, LogEventLevel.Error);
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
