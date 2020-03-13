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

        private WebsiteView _editor;
        public WebsiteView Editor
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
                    _backCommand = new RelayCommand(Back, () => !IsProcessing);
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
                    _refreshCommand = new RelayCommand(LoadData, () => !IsProcessing);
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
        }

        public bool Sort(params SortDescription[] sorts)
        {
            return TryRunAsync(async () =>
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

        public void AcceptSelectedItems(WebsiteView[] websites)
        {
            _websiteSelections = websites;

            RefreshToggleState();
        }

        #region Private Members

        private async Task LoadDataCoreAsync(SortDescription[] sorts = null, int page = 1)
        {
            if (sorts != null)
            {
                _websiteSorts = sorts;
            }

            var sort = _websiteSorts?.FirstOrDefault();

            var websites = await _persister.GetWebsitesAsync(KeywordsFilter, StatusFilter, EnabledFilter, false, page, sort?.PropertyName, sort?.Direction == ListSortDirection.Descending);

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
                Editor = null;
                ShowTestResult = false;
            }
            else
            {
                Editor = SelectedWebsite.Clone();

                TryRunAsync(async () =>
                {
                    var logs = await _persister.GetCrawlLogsAsync(websiteId: SelectedWebsite.Id);

                    CrawlLogs = new ObservableCollection<CrawlLog>(logs.Items);
                });
            }
        }

        private void Back()
        {
            NavigationCommands.BrowseBack.Execute(null, null);
        }

        private void Analyze()
        {
            TryRunAsync(async () =>
            {
                ProcessingStatus = "Processing";

                AppendOutput("Started analysis");

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

                        if (result.Redirected)
                        {
                            status = WebsiteStatus.WarningRedirected;
                            notes = "URL redirected";
                        }
                        else if (result.Catalogs.Length == 0)
                        {
                            status = WebsiteStatus.ErrorCatalogMissing;
                            notes = "No catalogs detected";
                        }
                        else
                        {
                            // assume the published date detected above will be always valid or null
                            var latestPublished = result.Catalogs.OrderByDescending(o => o.Published).FirstOrDefault()?.Published;
                            if (latestPublished == null)
                            {
                                status = WebsiteStatus.WarningNoDates;
                                notes = "No published date in catalog items";
                            }
                            else if (latestPublished < DateTime.Now.AddDays(_crawlingSettings.OutdateDaysAgo * -1))
                            {
                                status = WebsiteStatus.ErrorOutdate;
                                notes = $"Last published: {latestPublished}";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        status = WebsiteStatus.ErrorBroken;
                        notes = ex.Message;
                    }

                    if (status != WebsiteStatus.Normal)
                    {
                        AppendOutput($"{website.Name}: {notes}", LogEventLevel.Warning);
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
                    websitesQueue = _persister.GetWebsiteAnalysisQueueAsync(websitesQueue?.Items.Last().Id).Result;

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

                AppendOutput("Completed analysis");

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

            Editor = new WebsiteView
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
                var isNew = Editor.Id == 0;

                await _persister.SaveAsync(Editor);

                if (isNew)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var website = Editor.Clone();

                        Websites.Insert(0, website);

                        SelectedWebsite = website;
                    });
                }
                else
                {
                    Editor.Clone(SelectedWebsite);

                    RefreshToggleState();
                }
            });
        }

        private void RunTest()
        {
            TryRunAsync(async () =>
            {
                ShowTestResult = true;

                // test catalogs
                var result = await TestAsync(Editor.Home, Editor.ListPath);
                CatalogItems = new ObservableCollection<CatalogItem>(result.Catalogs);

                if (result.Redirected)
                {
                    AppendOutput("Url redirected", LogEventLevel.Warning);
                }

                // TODO: test pagination
            });
        }

        private async Task<TestResult> TestAsync(string url, string listPath)
        {
            var result = new TestResult();

            var data = await _httpClient.GetHtmlAsync(url);

            result.Redirected = data.IsRedirected;

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(data.Content);

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
                SelectedWebsite.Clone(Editor);
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
                var url = Utilities.ResolveResourceUrl(SelectedCatalogItem.Url, SelectedWebsite.Home);

                var data = await _httpClient.GetHtmlAsync(url);

                var article = Html2Article.GetArticle(data.Content);

                Article = new Article
                {
                    Url = url,
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
