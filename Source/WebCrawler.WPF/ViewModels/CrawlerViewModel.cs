using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using WebCrawler.Analyzers;
using WebCrawler.Common;
using WebCrawler.Crawlers;
using WebCrawler.DataLayer;
using WebCrawler.DTO;
using WebCrawler.Models;
using WebCrawler.WPF.Common;
using WebCrawler.WPF.Views;

namespace WebCrawler.WPF.ViewModels
{
    public class CrawlerViewModel : NotifyPropertyChanged
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICrawler _crawler;
        private readonly IDataLayer _dataLayer;
        private HttpClient _httpClient;
        private readonly CrawlSettings _crawlSettings;
        private readonly ILogger _logger;

        private readonly CollectionViewSource _crawlLogsSource;
        public ICollectionView CrawlLogsView
        {
            get
            {
                return _crawlLogsSource.View;
            }
        }

        #region Notify Properties

        public bool IsProcessing
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue(value); }
        }

        public bool IsInitializing
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue(value); }
        }

        public bool IsCrawling
        {
            get { return GetPropertyValue<bool>(); }
            set
            {
                if (!SetPropertyValue(value)) { return; }

                if (value)
                {
                    OnCrawlingStarted();
                }
                else
                {
                    OnCrawlingCompleted();
                }
            }
        }

        public string CrawlingStatus
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

                LoadCrawlLogs();
            }
        }

        public CrawlStatus StatusFilter
        {
            get { return GetPropertyValue<CrawlStatus>(); }
            set
            {
                if (!SetPropertyValue(value)) { return; }

                LoadCrawlLogs();
            }
        }

        public CrawlDTO SelectedCrawl
        {
            get { return GetPropertyValue<CrawlDTO>(); }
            set
            {
                if (!SetPropertyValue(value)) { return; }

                // TODO: This additional async call will cause the buttons state not refreshed
                LoadCrawlLogs();
            }
        }

        private CrawlLogDTO _selectedCrawlLog;
        public CrawlLogDTO SelectedCrawlLog
        {
            get { return GetPropertyValue<CrawlLogDTO>(); }
            set { SetPropertyValue(value); }
        }

        private ObservableCollection<CrawlDTO> _crawls;
        public ObservableCollection<CrawlDTO> Crawls
        {
            get { return GetPropertyValue<ObservableCollection<CrawlDTO>>(); }
            set { SetPropertyValue(value); }
        }

        private ObservableCollection<CrawlLogDTO> _crawlLogs;
        public ObservableCollection<CrawlLogDTO> CrawlLogs
        {
            get { return GetPropertyValue<ObservableCollection<CrawlLogDTO>>(); }
            set { SetPropertyValue(value); }
        }

        private PageInfo _pageInfo;
        public PageInfo PageInfo
        {
            get { return GetPropertyValue<PageInfo>(); }
            set { SetPropertyValue(value); }
        }

        private ObservableCollection<Output> _outputs;
        public ObservableCollection<Output> Outputs
        {
            get { return GetPropertyValue<ObservableCollection<Output>>(); }
            set { SetPropertyValue(value); }
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

        public CrawlerViewModel(IServiceProvider serviceProvider, ICrawler crawler, IDataLayer dataLayer, IHttpClientFactory clientFactory, CrawlSettings crawlSettings, ILogger<CrawlerViewModel> logger)
        {
            _crawler = crawler;
            _dataLayer = dataLayer;
            _crawlSettings = crawlSettings;
            _logger = logger;

            CrawlLogs = new ObservableCollection<CrawlLogDTO>();
            Outputs = new ObservableCollection<Output>();

            _crawlLogsSource = new CollectionViewSource { Source = CrawlLogs };
            _httpClient = clientFactory.CreateClient(Constants.HTTP_CLIENT_NAME_DEFAULT);
        }

        public void LoadData()
        {
            IsInitializing = true;

            TryRunAsync(async () =>
            {
                var crawls = await _dataLayer.GetCrawlsAsync();

                Crawls = new ObservableCollection<CrawlDTO>(crawls.Items);

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
            var logs = await _dataLayer.GetCrawlLogsAsync(SelectedCrawl.Id, null, KeywordsFilter, StatusFilter, page);

            App.Current.Dispatcher.Invoke(() =>
            {
                CrawlLogs.Clear();
                logs.Items.ForEach(o => CrawlLogs.Add(o));
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
                    var crawl = await _dataLayer.QueueCrawlAsync();

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Crawls.Insert(0, crawl);
                        SelectedCrawl = crawl;
                    });
                }
                else
                {
                    var firstCrawl = Crawls.First();
                    var crawl = await _dataLayer.ContinueCrawlAsync(firstCrawl.Id);

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
                CrawlLogDTO crawlLogDTO;

                ActionBlock<CrawlLogDTO> workerBlock = new ActionBlock<CrawlLogDTO>(async crawlLog =>
                {
                    crawlLogDTO = await CrawlAsync(crawlLog);

                    lock (this)
                    {
                        if (crawlLogDTO.Status == CrawlStatus.Completed)
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
                    MaxDegreeOfParallelism = _crawlSettings.MaxDegreeOfParallelism
                });

                PagedResult<CrawlLogDTO> crawlLogsQueue = null;
                do
                {
                    crawlLogsQueue = _dataLayer.GetCrawlingQueueAsync(SelectedCrawl.Id, crawlLogsQueue?.Items.Last().Id).Result;

                    if (total == 0)
                    {
                        total = crawlLogsQueue.PageInfo.ItemCount;

                        CrawlingStatus = $"Success: {SelectedCrawl.Success} Fail: {SelectedCrawl.Fail} Total: {total}";
                    }

                    foreach (var website in crawlLogsQueue.Items)
                    {
                        workerBlock.Post(website);

                        // accept queue items in the amount of batch size x 3
                        while (workerBlock.InputCount >= _crawlSettings.MaxDegreeOfParallelism * 2)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                } while (crawlLogsQueue.PageInfo.PageCount > 1);

                workerBlock.Complete();
                workerBlock.Completion.Wait();

                SelectedCrawl.Status = CrawlStatus.Completed;
                SelectedCrawl.Completed = DateTime.Now;

                await _dataLayer.SaveAsync(SelectedCrawl);

                AppendOutput($"Completed {(isFull ? "full" : "incremental")} crawl");

                IsCrawling = false;

                // reload data
                await LoadCrawlLogsCoreAsync();
            });
        }

        private async Task<CrawlLogDTO> CrawlAsync(CrawlLogDTO crawlLog)
        {
            crawlLog.Status = CrawlStatus.Crawling;
            crawlLog.Crawled = DateTime.Now;

            var articles = new List<Article>();

            App.Current.Dispatcher.Invoke(() => CrawlLogs.Insert(0, crawlLog));

            CatalogItem[] catalogItems = null;
            try
            {
                var data = await HtmlHelper.GetPageDataAsync(_httpClient, crawlLog.Website.Home, crawlLog.Website.CatalogRule);

                catalogItems = HtmlAnalyzer.DetectCatalogItems(data.Content, crawlLog.Website.CatalogRule, crawlLog.Website.ValidateDate);
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
                    .Take(Constants.MAX_RECORDS)
                    .ToArray();
            }
            catch (Exception ex)
            {
                crawlLog.Status = CrawlStatus.Failed;
                crawlLog.Notes = ex.Message;

                if (!(ex is HttpRequestException))
                {
                    _logger.LogError(ex, crawlLog.Website.Home);
                }
            }

            if (crawlLog.Status == CrawlStatus.Crawling)
            {
                foreach (var item in catalogItems)
                {
                    if (item.Url.Equals(crawlLog.LastHandled, StringComparison.CurrentCultureIgnoreCase))
                    {
                        break;
                    }

                    try
                    {
                        var data = await HtmlHelper.GetHtmlAsync(item.Url, _httpClient);
                        var info = Html2Article.GetArticle(data.Content);

                        articles.Add(new Article
                        {
                            Url = item.Url,
                            ActualUrl = data.IsRedirected ? data.ActualUrl : null,
                            Title = HtmlHelper.NormalizeText(info.Title),
                            Published = info.PublishDate ?? item.Published, // use date from article details page first
                            Content = HtmlHelper.NormalizeText(info.Content),
                            ContentHtml = HtmlHelper.NormalizeHtml(info.ContentWithTags, true),
                            WebsiteId = crawlLog.WebsiteId,
                            Timestamp = DateTime.Now
                        });

                        crawlLog.Success++;
                    }
                    catch (Exception ex)
                    {
                        AppendOutput(ex.Message, item.Url, LogLevel.Error);

                        if (!(ex is HttpRequestException))
                        {
                            _logger.LogError(ex, item.Url);
                        }

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
                var lastHandled = crawlLog.Status != CrawlStatus.Failed ? catalogItems[0].Url : null;

                using (var dataLayer = _serviceProvider.GetRequiredService<IDataLayer>())
                {
                    await dataLayer.SaveAsync(articles, crawlLog, lastHandled);
                }
            }
            catch (Exception ex)
            {
                crawlLog.Status = CrawlStatus.Failed;
                crawlLog.Notes = $"Failed to save data: {(ex.InnerException ?? ex).Message}";

                _logger.LogError(ex, crawlLog.Website.Home);
            }

            if (crawlLog.Status == CrawlStatus.Completed)
            {
                if (articles.Count == 0)
                {
                    AppendOutput("No updates", crawlLog.WebsiteHome, LogLevel.Information);
                }
                else
                {
                    AppendOutput("Completed website crawl", crawlLog.WebsiteHome, LogLevel.Information);
                }
            }
            else if (crawlLog.Status == CrawlStatus.Failed)
            {
                AppendOutput($"Failed to crawl website: {crawlLog.Notes}", crawlLog.WebsiteHome, LogLevel.Error);
            }

            return crawlLog;
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
            var websites = crawlLogs.Cast<CrawlLogDTO>().Select(o => o.WebsiteId).ToArray();
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
                    AppendOutput((ex.InnerException ?? ex).Message, null, LogLevel.Error);

                    _logger.LogError(ex, null);
                }
                finally
                {
                    IsProcessing = false;
                }
            });

            return true;
        }

        private void AppendOutput(string message, string url = null, LogLevel level = LogLevel.Information)
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
                _crawlLogsSource.LiveFilteringProperties.Add(nameof(CrawlLogDTO.Status));

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
            var crawlLog = args.Item as CrawlLogDTO;

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
