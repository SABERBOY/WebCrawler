using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;
using Serilog.Events;
using System;
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
        private static readonly object LOCK_DB = new object();

        private IPersister _persister;
        private HttpClient _httpClient;
        private CrawlingSettings _crawlingSettings;
        private Manage _managePage;

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
                    _manageCommand = new RelayCommand(Manage, () => !IsProcessing);
                }
                return _manageCommand;
            }
        }

        #endregion

        public CrawlerViewModel(IPersister persister, IHttpClientFactory clientFactory, CrawlingSettings crawlingSettings, Manage managePage)
        {
            _persister = persister;
            _httpClient = clientFactory.CreateClient(WebCrawler.Common.Constants.HTTP_CLIENT_NAME_DEFAULT);
            _crawlingSettings = crawlingSettings;
            _managePage = managePage;

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

                Crawls = new ObservableCollection<Crawl>(new Crawl[] { new Crawl() }.Concat(crawls.Items));

                SelectedCrawl = Crawls.First();

                IsInitializing = false;
            });
        }

        #region Private Members

        private void LoadCrawlLogs(int page = 1)
        {
            if (SelectedCrawl == null || SelectedCrawl.Id == 0)
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
            IsCrawling = true;

            TryRunAsync(async () =>
            {
                CrawlingStatus = "Processing";

                // create new crawl
                if (SelectedCrawl == null || SelectedCrawl.Id == 0)
                {
                    var crawl = await _persister.SaveAsync(default(Crawl));

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Crawls.Insert(0, crawl);
                        SelectedCrawl = crawl;
                    });
                }

                AppendOutput("Started crawl");

                int total = 0;
                CrawlLogView crawlLog;

                ActionBlock<Website> workerBlock = null;
                workerBlock = new ActionBlock<Website>(async website =>
                {
                    crawlLog = await CrawlAsync(website);

                    lock (this)
                    {
                        if (crawlLog.Status == CrawlStatus.Completed)
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

                int page = 1;
                PagedResult<Website> websites;
                do
                {
                    lock (LOCK_DB)
                    {
                        // TODO: consider to load the last crawl log only, for performance consideration
                        websites = _persister.GetWebsitesAsync(enabled: true, includeLogs: true, page: page, sortBy: nameof(Website.Id)).Result;
                    }

                    total = websites.PageInfo.ItemCount;

                    foreach (var website in websites.Items)
                    {
                        workerBlock.Post(website);

                        // accept queue items in the amount of batch size x 3
                        while (workerBlock.InputCount >= _crawlingSettings.MaxDegreeOfParallelism * 2)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                } while (page++ < websites.PageInfo.PageCount);

                workerBlock.Complete();
                workerBlock.Completion.Wait();

                SelectedCrawl.Status = CrawlStatus.Completed;
                SelectedCrawl.Completed = DateTime.Now;

                await _persister.SaveAsync(SelectedCrawl);

                AppendOutput("Completed crawl");

                IsCrawling = false;

                // reload data
                await LoadCrawlLogsCoreAsync();
            });
        }

        private async Task<CrawlLogView> CrawlAsync(Website website)
        {
            // start from last success crawl
            CrawlLog previousLog = website.CrawlLogs
                ?.Where(o => o.Status == CrawlStatus.Completed)
                .OrderByDescending(o => o.Id)
                .FirstOrDefault();

            CrawlLogView crawlLog = new CrawlLogView
            {
                CrawlId = SelectedCrawl.Id,
                WebsiteId = website.Id,
                WebsiteName = website.Name,
                WebsiteHome = website.Home,
                Status = CrawlStatus.Crawling,
                Crawled = DateTime.Now
            };
            List<Article> articles = new List<Article>();

            App.Current.Dispatcher.Invoke(() => CrawlLogs.Insert(0, crawlLog));

            CatalogItem[] catalogItems = null;
            try
            {
                var data = await _httpClient.GetHtmlAsync(website.Home);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(data);

                catalogItems = HtmlAnalyzer.ExtractCatalogItems(htmlDoc, website.ListPath);
                if (catalogItems.Length == 0)
                {
                    throw new Exception("Failed to locate catalog items");
                }

                // sort by published, as some website might have highlights always shown on the top
                catalogItems = catalogItems
                    .OrderByDescending(o => o.Published)
                    .ToArray();

                crawlLog.LastHandled = catalogItems[0].Url;
            }
            catch (Exception ex)
            {
                crawlLog.Status = CrawlStatus.Failed;
                crawlLog.Notes = ex.Message;
                crawlLog.LastHandled = previousLog?.LastHandled;
            }

            if (crawlLog.Status == CrawlStatus.Crawling)
            {
                foreach (var item in catalogItems)
                {
                    if (item.Url.Equals(previousLog?.LastHandled, StringComparison.CurrentCultureIgnoreCase))
                    {
                        break;
                    }

                    try
                    {
                        var html = await _httpClient.GetHtmlAsync(item.Url);
                        var info = Html2Article.GetArticle(html);

                        articles.Add(new Article
                        {
                            Url = item.Url,
                            Title = info.Title,
                            Published = info.PublishDate ?? item.Published, // use date from article details page first
                            Content = info.Content,
                            ContentHtml = info.ContentWithTags,
                            WebsiteId = website.Id,
                            Timestamp = DateTime.Now
                        });
                        
                        crawlLog.Success++;
                    }
                    catch (Exception ex)
                    {
                        AppendOutput(ex.Message, item.Url, LogEventLevel.Error);

                        crawlLog.Fail++;
                    }
                }

                if (crawlLog.Success == 0 && crawlLog.Fail > 0)
                {
                    crawlLog.Status = CrawlStatus.Failed;
                    crawlLog.Notes = "Failed as nothing succeeded";
                }
            }

            try
            {
                lock (LOCK_DB)
                {
                    _persister.SaveAsync(articles, crawlLog).Wait();
                }
            }
            catch (Exception ex)
            {
                crawlLog.Status = CrawlStatus.Failed;
                crawlLog.Notes = $"Failed to save data: {(ex.InnerException ?? ex).ToString()}";
            }

            if (crawlLog.Status == CrawlStatus.Completed)
            {
                if (articles.Count == 0)
                {
                    AppendOutput("No updates", website.Home, LogEventLevel.Information);
                }
                else
                {
                    AppendOutput($"Crawled website, success: {crawlLog.Success}, fail: {crawlLog.Fail}", website.Home, LogEventLevel.Information);
                }
            }
            else if (crawlLog.Status == CrawlStatus.Failed)
            {
                AppendOutput($"Failed to crawl website: {crawlLog.Notes}", website.Home, LogEventLevel.Error);
            }

            return crawlLog;
        }

        private void OnCrawlingStarted()
        {
            PageInfo = null;

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
            WPFUtilities.Navigate(_managePage);
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
