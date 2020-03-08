using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;
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
                    _crawlCommand = new RelayCommand(() => Crawl(), () => !IsProcessing);
                }
                return _crawlCommand;
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

            Outputs = new ObservableCollection<Output>();
        }

        public void LoadData()
        {
            TryRunAsync(async () =>
            {
                var crawls = await _persister.GetCrawlsAsync();

                Crawls = new ObservableCollection<Crawl>(new Crawl[] { new Crawl() }.Concat(crawls.Items));

                SelectedCrawl = Crawls.FirstOrDefault();
            });
        }

        #region Private Members

        private void LoadCrawlLogs(int page = 1)
        {
            TryRunAsync(async () =>
            {
                var logs = await _persister.GetCrawlLogsAsync(SelectedCrawl.Id, null, KeywordsFilter, StatusFilter, page);

                CrawlLogs = new ObservableCollection<CrawlLog>(logs.Items);
                PageInfo = logs.PageInfo;
            });
        }

        private void Crawl()
        {
            TryRunAsync(async () =>
            {
                if (SelectedCrawl == null)
                {
                    var crawl = await _persister.SaveAsync(default(Crawl));

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Crawls.Insert(0, crawl);
                        SelectedCrawl = crawl;
                    });
                }

                // TODO: start crawl
            });
        }

        private void Analyze()
        {
            TryRunAsync(async () =>
            {
                ProcessingStatus = "Processing";

                int processed = 0;
                int total = 0;

                ActionBlock<Website> workerBlock = null;
                workerBlock = new ActionBlock<Website>(async website =>
                {
                    WebsiteStatus status = WebsiteStatus.Normal;
                    string notes = null;

                    try
                    {
                        /*var catalogItems = await TestAsync(website.Home, website.ListPath);

                        if (catalogItems.Length == 0)
                        {
                            status = WebsiteStatus.CatalogMissing;
                            notes = "Couldn't detect the catalog items";
                        }
                        else
                        {
                            // assume the published date detected above will be always valid or null
                            var latestPublished = catalogItems.OrderByDescending(o => o.Published).FirstOrDefault()?.Published;
                            if (latestPublished != null && latestPublished < DateTime.Now.AddDays(_crawlingSettings.OutdateDaysAgo * -1))
                            {
                                status = WebsiteStatus.Outdate;
                                notes = $"Outdated as last published date: {latestPublished}";
                            }
                        }*/
                    }
                    catch (Exception ex)
                    {
                        status = WebsiteStatus.Broken;
                        notes = ex.Message;
                    }

                    if (status != WebsiteStatus.Normal)
                    {
                        AppendOutput($"Detected broken website: {website.Name}. {notes}", LogEventLevel.Warning);
                    }

                    try
                    {
                        lock (LOCK_DB)
                        {
                            //AppendOutput("Innert lock started: " + website.Home, LogEventLevel.Information);

                            _persister.UpdateStatusAsync(website.Id, status, notes).Wait();

                            //AppendOutput("Innert lock completed: " + website.Home, LogEventLevel.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendOutput($"{ex.Message} {website.Home}", LogEventLevel.Error);
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

                int page = 1;
                PagedResult<Website> websites;
                do
                {
                    lock (LOCK_DB)
                    {
                        //AppendOutput("Outer lock started", LogEventLevel.Information);

                        websites = _persister.GetWebsitesAsync(enabled: true, page: page, sortBy: nameof(Website.Id)).Result;

                        //AppendOutput("Outer lock completed", LogEventLevel.Information);
                    }

                    total = websites.PageInfo.ItemCount;

                    foreach (var website in websites.Items)
                    {
                        workerBlock.Post(website);

                        ProcessingStatus = $"Processing {workerBlock.InputCount}/{processed}/{total}";

                        // accept queue items in the amount of batch size x 3
                        while (workerBlock.InputCount >= _crawlingSettings.MaxDegreeOfParallelism * 2)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                } while (page++ < websites.PageInfo.PageCount);

                workerBlock.Complete();
                workerBlock.Completion.Wait();
            });
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
