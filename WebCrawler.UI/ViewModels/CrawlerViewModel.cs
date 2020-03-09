using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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
                ProcessingStatus = "Processing";

                // create new crawl
                if (SelectedCrawl.Id == 0)
                {
                    var crawl = await _persister.SaveAsync(default(Crawl));

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Crawls.Insert(0, crawl);
                        SelectedCrawl = crawl;
                    });
                }

                int processed = 0;
                int total = 0;

                ActionBlock<Website> workerBlock = null;
                workerBlock = new ActionBlock<Website>(async website =>
                {
                    await CrawlAsync(website);

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
                        websites = _persister.GetWebsitesAsync(status: WebsiteStatus.Normal, enabled: true, includeLogs: true, page: page, sortBy: nameof(Website.Id)).Result;
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
                    break;
                } while (page++ < websites.PageInfo.PageCount);

                workerBlock.Complete();
                workerBlock.Completion.Wait();
            });
        }

        private async Task CrawlAsync(Website website)
        {
            CrawlLog previousLog = website.CrawlLogs?.OrderByDescending(o => o.Id).FirstOrDefault();

            CrawlLog crawlLog = new CrawlLog
            {
                CrawlId = SelectedCrawl.Id,
                WebsiteId = website.Id,
                Crawled = DateTime.Now
            };
            List<Article> articles = new List<Article>();

            CatalogItem[] catalogItems = null;
            try
            {
                var data = await _httpClient.GetHtmlAsync(website.Home);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(data);

                if (!string.IsNullOrEmpty(website.ListPath))
                {
                    catalogItems = HtmlAnalyzer.ExtractCatalogItems(htmlDoc, website.ListPath);
                }
                else
                {
                    var blocks = HtmlAnalyzer.EvaluateCatalogs(htmlDoc);
                    if (blocks.Length == 0)
                    {
                        throw new Exception("Failed to auto detect catalog");
                    }
                    else
                    {
                        catalogItems = HtmlAnalyzer.ExtractCatalogItems(htmlDoc, blocks[0]);
                    }
                }

                if (catalogItems.Length == 0)
                {
                    throw new Exception("Failed to locate catalog items");
                }

                crawlLog.LastHandled = catalogItems[0].Url;
            }
            catch (Exception ex)
            {
                crawlLog.Status = CrawlStatus.Failed;
                crawlLog.Notes = ex.Message;
                crawlLog.LastHandled = previousLog?.LastHandled;
            }

            if (crawlLog.Status != CrawlStatus.Failed)
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

                        crawlLog.Failed++;
                    }
                }
            }

            lock (LOCK_DB)
            {
                _persister.SaveAsync(articles, crawlLog).Wait();
            }

            if (crawlLog.Status == CrawlStatus.Completed)
            {
                if (articles.Count == 0)
                {
                    AppendOutput($"Skipped website as no updates: {website.Home}", website.Home, LogEventLevel.Information);
                }
                else
                {
                    AppendOutput($"Crawled website (success: {crawlLog.Success}, failed: {crawlLog.Failed}): {website.Home}", website.Home, LogEventLevel.Information);
                }
            }
            else if (crawlLog.Status == CrawlStatus.Failed)
            {
                AppendOutput($"Failed to crawl article from website: {website.Home}", website.Home, LogEventLevel.Error);
            }

            App.Current.Dispatcher.Invoke(() => CrawlLogs.Insert(0, crawlLog));
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
                    AppendOutput(ex.Message, null, LogEventLevel.Error);
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
    }
}
