using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using WebCrawler.Common;
using WebCrawler.Common.Analyzers;
using WebCrawler.UI.Common;
using WebCrawler.UI.Crawlers;
using WebCrawler.UI.Models;
using WebCrawler.UI.Persisters;
using WebCrawler.UI.Views;

namespace WebCrawler.UI.ViewModels
{
    public class CrawlerViewModel : NotifyPropertyChanged
    {
        private IServiceProvider _serviceProvider;
        private IPersister _persister;
        private ILogger _logger;
        private HttpClient _httpClient;
        private CrawlingSettings _crawlingSettings;

        private CollectionViewSource _crawlLogsSource;
        public ICollectionView CrawlLogsView
        {
            get
            {
                return _crawlLogsSource.View;
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

        private bool _isInitializing;
        public bool IsInitializing
        {
            get { return _isInitializing; }
            set
            {
                if (_isInitializing == value) { return; }

                _isInitializing = value;
                RaisePropertyChanged();
            }
        }

        private bool _isCrawling;
        public bool IsCrawling
        {
            get { return _isCrawling; }
            set
            {
                if (_isCrawling == value) { return; }

                _isCrawling = value;
                RaisePropertyChanged();

                if (_isCrawling)
                {
                    OnCrawlingStarted();
                }
                else
                {
                    OnCrawlingCompleted();
                }
            }
        }

        private string _crawlingStatus;
        public string CrawlingStatus
        {
            get { return _crawlingStatus; }
            set
            {
                if (_crawlingStatus == value) { return; }

                _crawlingStatus = value;
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

                LoadCrawlLogs();
            }
        }

        private CrawlStatus _statusFilter;
        public CrawlStatus StatusFilter
        {
            get { return _statusFilter; }
            set
            {
                if (_statusFilter == value) { return; }

                _statusFilter = value;
                RaisePropertyChanged();

                LoadCrawlLogs();
            }
        }

        private Crawl _selectedCrawl;
        public Crawl SelectedCrawl
        {
            get { return _selectedCrawl; }
            set
            {
                if (_selectedCrawl == value) { return; }

                _selectedCrawl = value;
                RaisePropertyChanged();

                // TODO: This additional async call will cause the buttons state not refreshed
                LoadCrawlLogs();
            }
        }

        private CrawlLogView _selectedCrawlLog;
        public CrawlLogView SelectedCrawlLog
        {
            get { return _selectedCrawlLog; }
            set
            {
                if (_selectedCrawlLog == value) { return; }

                _selectedCrawlLog = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<Crawl> _crawls;
        public ObservableCollection<Crawl> Crawls
        {
            get { return _crawls; }
            set
            {
                if (_crawls == value) { return; }

                _crawls = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<CrawlLogView> _crawlLogs;
        public ObservableCollection<CrawlLogView> CrawlLogs
        {
            get { return _crawlLogs; }
            set
            {
                if (_crawlLogs == value) { return; }

                _crawlLogs = value;
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

        private RelayCommand _crawlCommand;
        public ICommand CrawlCommand
        {
            get
            {
                if (_crawlCommand == null)
                {
                    _crawlCommand = new RelayCommand(Crawl, () => !IsProcessing && !IsCrawling);
                }
                return _crawlCommand;
            }
        }

        private RelayCommand _stopCommand;
        public ICommand StopCommand
        {
            get
            {
                if (_stopCommand == null)
                {
                    _stopCommand = new RelayCommand(Stop, () => IsCrawling);
                }
                return _stopCommand;
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

        private RelayCommand _manageCommand;
        public ICommand ManageCommand
        {
            get
            {
                if (_manageCommand == null)
                {
                    _manageCommand = new RelayCommand(Manage);
                }
                return _manageCommand;
            }
        }

        private RelayCommand<IList> _manageSelectedCommand;
        public ICommand ManageSelectedCommand
        {
            get
            {
                if (_manageSelectedCommand == null)
                {
                    _manageSelectedCommand = new RelayCommand<IList>(ManageSelected, (crawlLogs) => SelectedCrawlLog != null);
                }
                return _manageSelectedCommand;
            }
        }

        #endregion

        public CrawlerViewModel(IServiceProvider serviceProvider, IPersister persister, ILogger logger, IHttpClientFactory clientFactory, CrawlingSettings crawlingSettings)
        {
            _serviceProvider = serviceProvider;
            _persister = persister;
            _logger = logger;
            _httpClient = clientFactory.CreateClient(WebCrawler.Common.Constants.HTTP_CLIENT_NAME_DEFAULT);
            _crawlingSettings = crawlingSettings;

            CrawlLogs = new ObservableCollection<CrawlLogView>();
            Outputs = new ObservableCollection<Output>();

            _crawlLogsSource = new CollectionViewSource { Source = CrawlLogs };
        }

        public void LoadData()
        {
            IsInitializing = true;

            TryRunAsync(async () =>
            {
                var crawls = await _persister.GetCrawlsAsync();

                Crawls = new ObservableCollection<Crawl>(crawls.Items);

                SelectedCrawl = Crawls.FirstOrDefault();

                IsInitializing = false;
            });
        }

        #region Private Members

        private void LoadCrawlLogs(int page = 1)
        {
            if (SelectedCrawl == null)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    CrawlLogs.Clear();
                    PageInfo = null;
                });
            }
            else if (IsCrawling)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    // WORKAROUND: Live fitering wouldn't affect the exiting records, until a change happen, e.g. notify property is updated,
                    // so let's refresh the view manually
                    CrawlLogsView.Refresh();
                });
            }
            else
            {
                TryRunAsync(async () =>
                {
                    await LoadCrawlLogsCoreAsync(page);
                });
            }
        }

        private async Task LoadCrawlLogsCoreAsync(int page = 1)
        {
            var logs = await _persister.GetCrawlLogsAsync(SelectedCrawl.Id, null, KeywordsFilter, StatusFilter, page);

            App.Current.Dispatcher.Invoke(() =>
            {
                CrawlLogs.Clear();
                logs.Items.Select(o => new CrawlLogView(o)).ForEach(o => CrawlLogs.Add(o));
                PageInfo = logs.PageInfo;
            });
        }

        private void Crawl()
        {
            var dialogResult = MessageBox.Show("Start full crawl?", "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (dialogResult == MessageBoxResult.Cancel)
            {
                return;
            }

            var isFull = dialogResult == MessageBoxResult.Yes || Crawls.Count == 0;

            TryRunAsync(async () =>
            {
                CrawlingStatus = "Processing";

                if (isFull)
                {
                    var crawl = await _persister.QueueCrawlAsync();

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Crawls.Insert(0, crawl);
                        SelectedCrawl = crawl;
                    });
                }
                else
                {
                    var firstCrawl = Crawls.First();
                    var crawl = await _persister.ContinueCrawlAsync(firstCrawl.Id);

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Crawls.RemoveAt(0);
                        Crawls.Insert(0, crawl);
                        SelectedCrawl = crawl;
                    });
                }

                IsCrawling = true;

                AppendOutput($"Started {(isFull ? "full" : "incremental")} crawl");

                int total = 0;
                CrawlLogView crawlLogView;

                ActionBlock<CrawlLog> workerBlock = new ActionBlock<CrawlLog>(async crawlLog =>
                {
                    crawlLogView = await CrawlAsync(crawlLog);

                    lock (this)
                    {
                        if (crawlLogView.Status == CrawlStatus.Completed)
                        {
                            SelectedCrawl.Success++;
                        }
                        else
                        {
                            SelectedCrawl.Fail++;
                        }
                    }

                    CrawlingStatus = $"Success: {SelectedCrawl.Success} Fail: {SelectedCrawl.Fail} Total: {total}";
                }, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _crawlingSettings.MaxDegreeOfParallelism
                });

                PagedResult<CrawlLog> crawlLogsQueue = null;
                do
                {
                    crawlLogsQueue = _persister.GetCrawlingQueueAsync(SelectedCrawl.Id, crawlLogsQueue?.Items.Last().Id).Result;

                    if (total == 0)
                    {
                        total = crawlLogsQueue.PageInfo.ItemCount;

                        CrawlingStatus = $"Success: {SelectedCrawl.Success} Fail: {SelectedCrawl.Fail} Total: {total}";
                    }

                    foreach (var website in crawlLogsQueue.Items)
                    {
                        workerBlock.Post(website);

                        // accept queue items in the amount of batch size x 3
                        while (workerBlock.InputCount >= _crawlingSettings.MaxDegreeOfParallelism * 2)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                } while (crawlLogsQueue.PageInfo.PageCount > 1);

                workerBlock.Complete();
                workerBlock.Completion.Wait();

                SelectedCrawl.Status = CrawlStatus.Completed;
                SelectedCrawl.Completed = DateTime.Now;

                await _persister.SaveAsync(SelectedCrawl);

                AppendOutput($"Completed {(isFull ? "full" : "incremental")} crawl");

                IsCrawling = false;

                // reload data
                await LoadCrawlLogsCoreAsync();
            });
        }

        private async Task<CrawlLogView> CrawlAsync(CrawlLog crawlLog)
        {
            var crawlLogView = new CrawlLogView(crawlLog)
            {
                Status = CrawlStatus.Crawling,
                Crawled = DateTime.Now
            };

            var articles = new List<Article>();

            App.Current.Dispatcher.Invoke(() => CrawlLogs.Insert(0, crawlLogView));

            CatalogItem[] catalogItems = null;
            try
            {
                var data = await _httpClient.GetHtmlAsync(crawlLog.Website.Home);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(data.Content);

                catalogItems = HtmlAnalyzer.DetectCatalogItems(htmlDoc, crawlLog.Website.ListPath, crawlLog.Website.ValidateDate);
                if (catalogItems.Length == 0)
                {
                    throw new Exception("Failed to locate catalog items");
                }

                if (catalogItems.All(o => o.HasDate))
                {
                    // sort by published, as some website might have highlights always shown on the top
                    catalogItems = catalogItems
                        .OrderByDescending(o => o.Published)
                        .ToArray();
                }

                // take the first x records only as some list might contains thousands of records
                catalogItems = catalogItems
                    .Take(Common.Constants.MAX_RECORDS)
                    .ToArray();
            }
            catch (Exception ex)
            {
                crawlLogView.Status = CrawlStatus.Failed;
                crawlLogView.Notes = ex.Message;

                if (!(ex is HttpRequestException))
                {
                    _logger.LogError(ex, crawlLog.Website.Home);
                }
            }

            if (crawlLogView.Status == CrawlStatus.Crawling)
            {
                foreach (var item in catalogItems)
                {
                    if (item.Url.Equals(crawlLog.LastHandled, StringComparison.CurrentCultureIgnoreCase))
                    {
                        break;
                    }

                    try
                    {
                        var data = await _httpClient.GetHtmlAsync(item.Url);
                        var info = Html2Article.GetArticle(data.Content);

                        articles.Add(new Article
                        {
                            Url = item.Url,
                            ActualUrl = data.IsRedirected ? data.ActualUrl : null,
                            Title = Utilities.NormalizeText(info.Title),
                            Published = info.PublishDate ?? item.Published, // use date from article details page first
                            Content = Utilities.NormalizeText(info.Content),
                            ContentHtml = Utilities.NormalizeHtml(info.ContentWithTags, true),
                            WebsiteId = crawlLog.WebsiteId,
                            Timestamp = DateTime.Now
                        });

                        crawlLogView.Success++;
                    }
                    catch (Exception ex)
                    {
                        AppendOutput(ex.Message, item.Url, LogEventLevel.Error);

                        if (!(ex is HttpRequestException))
                        {
                            _logger.LogError(ex, item.Url);
                        }

                        crawlLogView.Fail++;
                    }
                }

                if (crawlLogView.Success == 0 && crawlLogView.Fail > 0)
                {
                    crawlLogView.Status = CrawlStatus.Failed;
                    crawlLogView.Notes = "Failed as nothing succeeded";
                }
            }

            try
            {
                var lastHandled = crawlLogView.Status != CrawlStatus.Failed ? catalogItems[0].Url : null;

                using (var persister = _serviceProvider.GetRequiredService<IPersister>())
                {
                    await persister.SaveAsync(articles, crawlLogView, lastHandled);
                }
            }
            catch (Exception ex)
            {
                crawlLogView.Status = CrawlStatus.Failed;
                crawlLogView.Notes = $"Failed to save data: {(ex.InnerException ?? ex).Message}";

                _logger.LogError(ex, crawlLog.Website.Home);
            }

            if (crawlLogView.Status == CrawlStatus.Completed)
            {
                if (articles.Count == 0)
                {
                    AppendOutput("No updates", crawlLogView.WebsiteHome, LogEventLevel.Information);
                }
                else
                {
                    AppendOutput("Completed website crawl", crawlLogView.WebsiteHome, LogEventLevel.Information);
                }
            }
            else if (crawlLogView.Status == CrawlStatus.Failed)
            {
                AppendOutput($"Failed to crawl website: {crawlLogView.Notes}", crawlLogView.WebsiteHome, LogEventLevel.Error);
            }

            return crawlLogView;
        }

        private void OnCrawlingStarted()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                CrawlLogs.Clear();
                PageInfo = null;
            });

            KeywordsFilter = default;
            StatusFilter = default;

            RegisterLiveFiltering();
        }

        private void OnCrawlingCompleted()
        {
            UnregisterLiveFiltering();
        }

        private void Stop()
        {
            MessageBox.Show("Stop crawl hasn't been implemented yet");
        }

        private void Navigate()
        {
            LoadCrawlLogs(PageInfo?.CurrentPage ?? 1);
        }

        private void Manage()
        {
            Navigator.Navigate<Manage>();
        }

        private void ManageSelected(IList crawlLogs)
        {
            var websites = crawlLogs.Cast<CrawlLogView>().Select(o => o.WebsiteId).ToArray();
            Navigator.Navigate<Manage>(websites);
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
                    AppendOutput((ex.InnerException ?? ex).Message, null, LogEventLevel.Error);

                    _logger.LogError(ex, null);
                }
                finally
                {
                    IsProcessing = false;
                }
            });

            return true;
        }

        private void AppendOutput(string message, string url = null, LogEventLevel level = LogEventLevel.Information)
        {
            lock (this)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Outputs.Insert(0, new Output
                    {
                        Level = level,
                        URL = url,
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


        #region Live Filtering

        private void RegisterLiveFiltering()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _crawlLogsSource.Filter += CrawlLogsFilter;

                // configure properties which might be updated after being added to the collection and might be used in filtering
                _crawlLogsSource.LiveFilteringProperties.Add(nameof(CrawlLogView.Status));

                _crawlLogsSource.IsLiveFilteringRequested = true;
            });
        }

        private void UnregisterLiveFiltering()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _crawlLogsSource.IsLiveFilteringRequested = false;
                _crawlLogsSource.LiveFilteringProperties.Clear();
                _crawlLogsSource.Filter -= CrawlLogsFilter;
            });
        }

        private void CrawlLogsFilter(object sender, FilterEventArgs args)
        {
            var crawlLog = args.Item as CrawlLogView;

            if (IsCrawling)
            {
                args.Accepted = (string.IsNullOrEmpty(KeywordsFilter) || crawlLog.WebsiteName.Contains(KeywordsFilter) || crawlLog.WebsiteHome.Contains(KeywordsFilter))
                    && (StatusFilter == CrawlStatus.All || crawlLog.Status == StatusFilter);
            }
            else
            {
                args.Accepted = true;
            }
        }

        #endregion
    }
}
