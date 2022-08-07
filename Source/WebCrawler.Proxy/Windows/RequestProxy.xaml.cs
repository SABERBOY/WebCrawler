using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WebCrawler.Proxy.Common;
using WebCrawler.Queue;

namespace WebCrawler.Proxy.Windows
{
    public partial class RequestProxy : Window
    {
        private readonly object _LOCK = new object();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly DispatcherTimer _timer;

        private readonly IProxyDispatcher _proxyDispatcher;
        private readonly TaskbarIcon _taskbarIcon;

        private AjaxProxyRequest _request;
        private int _countdown;
        private bool _detected;
        private DownloadResult _result;

        public bool EnableRequestProxyViewSource { get; set; }

        public RequestProxy(IProxyDispatcher proxyDispatcher, TaskbarIcon taskbarIcon, bool enableRequestProxyViewSource = false)
        {
            InitializeComponent();

            _proxyDispatcher = proxyDispatcher;
            _taskbarIcon = taskbarIcon;
            EnableRequestProxyViewSource = enableRequestProxyViewSource;

            // https://referencesource.microsoft.com/#PresentationFramework/src/Framework/MS/Internal/Controls/WebBrowserEvent.cs,284
            // https://referencesource.microsoft.com/#PresentationFramework/src/Framework/System/Windows/Controls/WebBrowser.cs,703

            webView.Loaded += WebView_Loaded;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _timer.Tick += Timer_Tick;

            AutoPosition();

            _proxyDispatcher.Register(ProxyDispatcher.QUEUE_REQUESTS, async (AjaxProxyRequest request) =>
            {
                _request = request;

                var result = await SendAsync(request);

                _proxyDispatcher.Send(request.PageUrl, result);
            }, default);

            Loaded += RequestProxy_Loaded;

            Console.WriteLine(Constants.ProxyReadyMessage);
            _taskbarIcon.ShowBalloonTip(null, Constants.ProxyReadyMessage, BalloonIcon.Info);
        }

        private void RequestProxy_Loaded(object sender, RoutedEventArgs e)
        {
            // hide the window once loaded
            this.Hide();
        }

        private async void WebView_Loaded(object sender, RoutedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async();

            webView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            webView.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
            webView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            Title = webView.CoreWebView2.DocumentTitle;
        }

        /// <summary>
        /// Notice: responses belong to previous page might still come when navigating to a different page (e.g. requests triggerred in DOM unload event)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CoreWebView2_WebResourceResponseReceived(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            lock (_LOCK)
            {
                // the response content stream is loaded async, the first response might not always be the success HTML page response
                if (_detected
                    || e.Response.StatusCode == 301
                    || e.Response.StatusCode == 302
                    || !Regex.IsMatch(e.Request.Uri, _request.AjaxUrlExp))
                {
                    return;
                }

                _detected = true;
            }

            _timer.Stop();

            _result.ResponseUri = e.Request.Uri;
            _result.ContentType = e.Response.GetContentType();
            _result.StatusCode = (HttpStatusCode)e.Response.StatusCode;

            try
            {
                if (EnableRequestProxyViewSource)
                {
                    // NOTE: Content would be loaded in CoreWebView2_DOMContentLoaded later,
                    // as the content DOM might not be ready at this time
                }
                else
                {
                    using (var stream = await e.Response.GetContentAsync())
                    {
                        if (stream != null)
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                _result.Content = await reader.ReadToEndAsync();
                            }
                        }
                        else
                        {
                            _result.Exception = new Exception("Couldn't detect the page content via proxy.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _result.ResponseUri = e.Request.Uri;
                _result.Exception = ex;
            }
        }

        private async void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (_result.Content == null && EnableRequestProxyViewSource)
            {
                _result.Content = await webView.GetContentAsync();
            }
        }

        public async Task<DownloadResult> SendAsync(AjaxProxyRequest request)
        {
            await _semaphore.WaitAsync();

            lock (_LOCK)
            {
                _detected = false;
            }
            _result = new DownloadResult
            {
                RequestUri = request.PageUrl
            };
            _countdown = request.TimeoutSeconds;
            _timer.Start();

            var urlForBrowser = EnableRequestProxyViewSource ? ("view-source:" + request.PageUrl) : request.PageUrl;
            App.Current.Dispatcher.Invoke(() => { webView.Source = new Uri(urlForBrowser); });

            return await Task.Run(() =>
            {
                try
                {
                    while (_result.Content == null && _result.Exception == null)
                    {
                        Thread.Sleep(100);
                    }

                    App.Current.Dispatcher.Invoke(() => { webView.Source = new Uri("about:blank"); });

                    return _result;
                }
                finally
                {
                    _semaphore.Release();
                }
            }).ConfigureAwait(false);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (--_countdown <= 0)
            {
                txtCountdown.Text = null;

                _timer.Stop();
                _result.Exception = new Exception("RequestProxy: Exceeded count down.");
            }
            else
            {
                txtCountdown.Text = _countdown.ToString();
            }
        }

        private void AutoPosition()
        {
            Left = SystemParameters.FullPrimaryScreenWidth - Width;
            Top = SystemParameters.FullPrimaryScreenHeight - Height + SystemParameters.WindowCaptionHeight;
        }
    }
}
