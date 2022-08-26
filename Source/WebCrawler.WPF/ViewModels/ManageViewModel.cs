using GalaSoft.MvvmLight.Command;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using WebCrawler.Analyzers;
using WebCrawler.Common;
using WebCrawler.Crawlers;
using WebCrawler.DataLayer;
using WebCrawler.DTO;
using WebCrawler.Models;
using WebCrawler.Queue;
using WebCrawler.WPF.Dialogs;

namespace WebCrawler.WPF.ViewModels
{
    public class ManageViewModel : NotifyPropertyChanged
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDataLayer _dataLayer;
        private readonly IProxyDispatcher _proxyDispatcher;
        private readonly HttpClient _httpClient;
        private readonly CrawlSettings _crawlSettings;
        private readonly ILogger _logger;

        private SortDescription[] _websiteSorts;
        private WebsiteDTO[] _websiteSelections;

        #region Notify Properties

        public bool IsProcessing
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue(value); }
        }

        public string ProcessingStatus
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public string KeywordsFilter
        {
            get { return GetPropertyValue<string>(); }
            set
            {
                if (!SetPropertyValue(value)) { return; }

                LoadData();
            }
        }

        public WebsiteStatus StatusFilter
        {
            get { return GetPropertyValue<WebsiteStatus>(); }
            set
            {
                if (!SetPropertyValue(value)) { return; }

                LoadData();
            }
        }

        public bool? EnabledFilter
        {
            get { return GetPropertyValue<bool?>(); }
            set
            {
                if (!SetPropertyValue(value)) { return; }

                LoadData();
            }
        }

        public WebsiteDTO SelectedWebsite
        {
            get { return GetPropertyValue<WebsiteDTO>(); }
            set
            {
                if (!SetPropertyValue(value)) { return; }

                // TODO: This additional async call will cause the buttons state not refreshed
                OnSelectedWebsiteChanged();
            }
        }

        public WebsiteEditor Editor
        {
            get { return GetPropertyValue<WebsiteEditor>(); }
            set { SetPropertyValue(value); }
        }

        public Output SelectedOutput
        {
            get { return GetPropertyValue<Output>(); }
            set { SetPropertyValue(value); }
        }

        public int SelectedViewIndex
        {
            get { return GetPropertyValue<int>(); }
            set { SetPropertyValue(value); }
        }

        public CatalogItem SelectedCatalogItem
        {
            get { return GetPropertyValue<CatalogItem>(); }
            set
            {
                if (!SetPropertyValue(value)) { return; }

                OnSelectedCatalogItemChanged();
            }
        }

        public Article Article
        {
            get { return GetPropertyValue<Article>(); }
            set { SetPropertyValue(value); }
        }

        public bool? ToggleAsEnable
        {
            get { return GetPropertyValue<bool?>(); }
            set { SetPropertyValue(value); }
        }

        /// <summary>
        /// Couldn't always create new Websites instance (which don't require Dispatcher Invoke) here as we want to persist the sort arrows in the data grid
        /// </summary>
        public ObservableCollection<WebsiteDTO> Websites
        {
            get { return GetPropertyValue<ObservableCollection<WebsiteDTO>>(); }
            set { SetPropertyValue(value); }
        }

        public PageInfo PageInfo
        {
            get { return GetPropertyValue<PageInfo>(); }
            set { SetPropertyValue(value); }
        }

        public ObservableCollection<CrawlLogDTO> CrawlLogs
        {
            get { return GetPropertyValue<ObservableCollection<CrawlLogDTO>>(); }
            set { SetPropertyValue(value); }
        }

        public ObservableCollection<CatalogItem> CatalogItems
        {
            get { return GetPropertyValue<ObservableCollection<CatalogItem>>(); }
            set { SetPropertyValue(value); }
        }

        public ObservableCollection<Output> Outputs
        {
            get { return GetPropertyValue<ObservableCollection<Output>>(); }
            set { SetPropertyValue(value); }
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

        private RelayCommand _duplicateSelectedCommand;
        public ICommand DuplicateSelectedCommand
        {
            get
            {
                if (_duplicateSelectedCommand == null)
                {
                    _duplicateSelectedCommand = new RelayCommand(DuplicateSelected, () => !IsProcessing);
                }
                return _duplicateSelectedCommand;
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

        private RelayCommand _addRuleCommand;
        public ICommand AddRuleCommand
        {
            get
            {
                if (_addRuleCommand == null)
                {
                    _addRuleCommand = new RelayCommand(
                        () => ShowRuleEditor(),
                        () => !IsProcessing
                    );
                }
                return _addRuleCommand;
            }
        }

        private RelayCommand<WebsiteRuleDTO> _editRuleCommand;
        public ICommand EditRuleCommand
        {
            get
            {
                if (_editRuleCommand == null)
                {
                    _editRuleCommand = new RelayCommand<WebsiteRuleDTO>(
                        (WebsiteRuleDTO rule) => ShowRuleEditor(rule),
                        (WebsiteRuleDTO rule) => !IsProcessing
                    );
                }
                return _editRuleCommand;
            }
        }

        private RelayCommand<WebsiteRuleDTO> _removeRuleCommand;
        public ICommand RemoveRuleCommand
        {
            get
            {
                if (_removeRuleCommand == null)
                {
                    _removeRuleCommand = new RelayCommand<WebsiteRuleDTO>(
                        (WebsiteRuleDTO rule) => RemoveRule(rule),
                        (WebsiteRuleDTO rule) => !IsProcessing
                    );
                }
                return _removeRuleCommand;
            }
        }

        #endregion

        public ManageViewModel(IServiceProvider serviceProvider, IDataLayer dataLayer, IHttpClientFactory clientFactory, IProxyDispatcher proxyDispatcher, CrawlSettings crawlSettings, ILogger<ManageViewModel> logger)
        {
            _serviceProvider = serviceProvider;
            _dataLayer = dataLayer;
            _proxyDispatcher = proxyDispatcher;
            _logger = logger;
            _httpClient = clientFactory.CreateClient(Constants.HTTP_CLIENT_NAME_DEFAULT);
            _crawlSettings = crawlSettings;

            Websites = new ObservableCollection<WebsiteDTO>();
            Outputs = new ObservableCollection<Output>();
            Editor = new WebsiteEditor
            {
                Website = new WebsiteDTO()
            };
            Editor.Website.PropertyChanged += Website_PropertyChanged;

            EnabledFilter = true;
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

        public void AcceptSelectedItems(WebsiteDTO[] websites)
        {
            _websiteSelections = websites;

            RefreshToggleState();
        }

        #region Private Members

        private async Task LoadDataCoreAsync(SortDescription[] sorts = null, int[] websiteIds = null, int page = 1)
        {
            if (sorts != null)
            {
                _websiteSorts = sorts;
            }

            var sort = _websiteSorts?.FirstOrDefault();

            PagedResult<WebsiteDTO> websites;
            if (websiteIds == null || websiteIds.Length == 0)
            {
                websites = await _dataLayer.GetWebsitesAsync(KeywordsFilter, StatusFilter, EnabledFilter, false, page, sort?.PropertyName, sort?.Direction == ListSortDirection.Descending);
            }
            else
            {
                websites = new PagedResult<WebsiteDTO>
                {
                    Items = await _dataLayer.GetWebsitesAsync(websiteIds, false),
                    PageInfo = null
                };
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                // couldn't create new Websites instance (which don't require Dispatcher Invoke) here as we want to persist the sort arrows in the data grid
                Websites.Clear();
                foreach (var web in websites.Items)
                {
                    Websites.Add(web);
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
                    var logs = await _dataLayer.GetCrawlLogsAsync(websiteId: SelectedWebsite.Id);
                    CrawlLogs = new ObservableCollection<CrawlLogDTO>(logs.Items);

                    await LoadHtmlCoreAsync();
                });
            }
        }

        private async Task LoadHtmlCoreAsync()
        {
            Editor.Response = null;

            if (!string.IsNullOrEmpty(Editor.Website.Home) && Editor.Website.Status != WebsiteStatus.ErrorBroken)
            {
                Editor.Response = await HtmlHelper.GetPageDataAsync(_httpClient, _proxyDispatcher, Editor.Website.Home, Editor.Website.CatalogRule);

                HtmlHelper.HandleParseErrorsIfAny(Editor.HtmlDoc, (errors) => AppendOutput(errors, Editor.Website.Home, Editor.Website.Id, LogLevel.Warning));

                if (Editor.Response.IsRedirected)
                {
                    AppendOutput("Url redirected to: " + Editor.Response.ActualUrl, Editor.Website.Home, Editor.Website.Id, LogLevel.Warning);
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

                ActionBlock<WebsiteDTO> workerBlock = null;
                workerBlock = new ActionBlock<WebsiteDTO>(async website =>
                {
                    var result = await TestAsync(website);

                    try
                    {
                        using (var dataLayer = _serviceProvider.GetRequiredService<IDataLayer>())
                        {
                            var enabled = WebsiteDTO.DetermineWebsiteEnabledStatus(result.Status.Value, website.Status);

                            await dataLayer.UpdateStatusAsync(website.Id, result.Status, enabled, result.Notes);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendOutput((ex.InnerException ?? ex).Message, website.Home, website.Id, LogLevel.Error);

                        _logger.LogError(ex, website.Home);
                    }

                    lock (this)
                    {
                        processed++;
                    }

                    ProcessingStatus = $"Processing {workerBlock.InputCount}/{processed}/{total}";
                }, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _crawlSettings.MaxDegreeOfParallelism
                });

                PagedResult<WebsiteDTO> websitesQueue = null;
                do
                {
                    websitesQueue = _dataLayer.GetWebsiteAnalysisQueueAsync(isFull, websitesQueue?.Items.Last().Id).Result;

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
                        while (workerBlock.InputCount >= _crawlSettings.MaxDegreeOfParallelism * 2)
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
                await _dataLayer.ToggleAsync(ToggleAsEnable.Value, _websiteSelections.Select(o => o.Id).ToArray());

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
                await _dataLayer.DeleteAsync(_websiteSelections.Select(o => o.Id).ToArray());

                App.Current.Dispatcher.Invoke(() =>
                {
                    _websiteSelections.ForEach(o => Websites.Remove(o));
                });
            });
        }

        private void DuplicateSelected()
        {
            TryRunAsync(async () =>
            {
                var duplicates = await _dataLayer.DuplicateAsync(_websiteSelections.Select(o => o.Id).ToArray());

                App.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var duplicate in duplicates)
                    {
                        Websites.Insert(0, duplicate);
                    }

                    SelectedWebsite = duplicates[duplicates.Length - 1];
                });
            });
        }

        private void Add()
        {
            SelectedWebsite = null;
            Editor.IsEditing = true;

            new WebsiteDTO
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

                await _dataLayer.SaveAsync(Editor.Website);

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
                var result = await TestAsync(Editor.Website, Editor.Response);
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

        private async Task<TestResult> TestAsync(WebsiteDTO website, ResponseData response = null)
        {
            var result = new TestResult { Status = WebsiteStatus.Normal };

            try
            {
                if (response == null)
                {
                    result.CatalogsResponse = await HtmlHelper.GetPageDataAsync(_httpClient, _proxyDispatcher, website.Home, website.CatalogRule);
                }
                else
                {
                    result.CatalogsResponse = response;
                }

                // suppress HTML parsing error in TestAsync as there're too many warnings, such error will still be handled in single website mode
                //if (response == null)
                //{
                //    // show parsing errors only when the HTML is requested in this scope
                //    htmlDoc.HandleParseErrorsIfAny((errors) => AppendOutput(errors, url, LogLevel.Warning));
                //}

                result.Catalogs = HtmlAnalyzer.DetectCatalogItems(result.CatalogsResponse.Content, website.CatalogRule, website.ValidateDate);

                if (result.CatalogsResponse.IsRedirected)
                {
                    result.Status = WebsiteStatus.WarningRedirected;
                    result.Notes = "URL redirected to: " + result.CatalogsResponse.ActualUrl;
                }
                else if (result.Catalogs.Length == 0)
                {
                    result.Status = WebsiteStatus.ErrorCatalogMissing;
                    result.Notes = "No catalogs detected";
                }
                else if (website.ValidateDate)
                {
                    if (result.Catalogs.Any(o => !o.HasDate))
                    {
                        result.Status = WebsiteStatus.WarningNoDates;
                        result.Notes = "No published date in catalog items";
                    }
                    else
                    {
                        var latestItem = result.Catalogs.OrderByDescending(o => o.Published).FirstOrDefault();
                        if (latestItem.Published != null && latestItem.Published < DateTime.Now.AddDays(_crawlSettings.OutdateDaysAgo * -1))
                        {
                            result.Status = WebsiteStatus.ErrorOutdate;
                            result.Notes = $"Last published: {latestItem.Published}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // treat unavailable websites as broken
                if (ex.DetermineWebsiteBroken())
            {
                var baseEx = ex.GetBaseException();

                result.Status = WebsiteStatus.ErrorBroken;
                result.Notes = $"{baseEx.GetType().Name}: {baseEx.Message}";
            }
                else
            {
                    // keep previous status as it might not be a website issue as usual, e.g. TaskCanceledException
                result.Status = null;
                result.Notes = ex.Message;
                }

                _logger.LogError(ex, website.Home);
            }

            // output for changes only
            if (result.Status == null)
            {
                if (!string.IsNullOrEmpty(result.Notes))
                {
                    AppendOutput(result.Notes, website.Home, website.Id, LogLevel.Warning);
                }
            }
            else if (result.Status != website.Status || result.Notes != website.SysNotes)
            {
                var message = string.IsNullOrWhiteSpace(result.Notes) ? $"{result.Status}" : $"{result.Status} | {result.Notes}";
                var level = result.Status == WebsiteStatus.Normal ? LogLevel.Information : LogLevel.Warning;

                AppendOutput(message, website.Home, website.Id, level);
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
                await _dataLayer.DeleteAsync(Editor.Website.Id);

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

                var website = await _dataLayer.GetAsync<Website>(SelectedOutput.WebsiteId.Value);

                new WebsiteDTO(website).CloneTo(Editor.Website);

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
                var data = await HtmlHelper.GetPageDataAsync(_httpClient, _proxyDispatcher, SelectedCatalogItem.Url, Editor.Website.ArticleRule);

                var article = HtmlAnalyzer.ParseArticle(data.Content, Editor.Website.ArticleRule);

                Article = new Article
                {
                    Url = SelectedCatalogItem.Url,
                    Title = article.Title,
                    Content = article.Content,
                    Published = article.PublishDate,
                    Author = article.Author
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

        private void Website_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WebsiteDTO.Home))
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
                    AppendOutput((ex.InnerException ?? ex).Message, level: LogLevel.Error);

                    _logger.LogError(ex, null);
                }
                finally
                {
                    IsProcessing = false;
                }
            });

            return true;
        }

        private void AppendOutput(string message, string url = null, int? websiteId = null, LogLevel level = LogLevel.Information)
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
                        Outputs.RemoveAt(Outputs.Count - 1);
                    }
                });
            }
        }

        private void ShowRuleEditor(WebsiteRuleDTO rule = null)
        {
            var previous = rule?.Type != WebsiteRuleType.Catalog ? null : rule.DeepCopy();

            var dialog = new RuleEditor(Editor.Website, Editor.HtmlDoc, rule);
            dialog.ShowDialog();

            if (dialog.Rule.Type == WebsiteRuleType.Catalog)
            {
                if (previous == null
                    || dialog.Rule.PageLoadOption != previous.PageLoadOption
                    || dialog.Rule.PageUrlReviseExp != previous.PageUrlReviseExp
                    || dialog.Rule.PageUrlReplacement != previous.PageUrlReplacement)
                {
                    // reset response as the page load behavior is changed
                    Editor.Response = null;
                }
            }
        }

        private void RemoveRule(WebsiteRuleDTO rule)
        {
            Editor.Website.Rules.Remove(rule);
        }

        #endregion
    }
}
