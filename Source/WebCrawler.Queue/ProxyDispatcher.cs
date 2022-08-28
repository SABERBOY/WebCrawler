using Cloudtoid.Interprocess;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace WebCrawler.Queue
{
    public class ProxyDispatcher : IProxyDispatcher
    {
        public const string QUEUE_REQUESTS = "requests";
        public const long QUEUES_CAPACITY_BYTES = 10L * 1024 * 1024;
        private const int PROXY_START_TIMEOUT_SECONDS = 30;

        private readonly ProxySettings _proxySettings;
        private readonly IQueueFactory _queueFactory;
        private readonly ILogger _logger;

        private bool _isProxyReady;

        public ProxyDispatcher(ProxySettings proxySettings, IQueueFactory queueFactory, ILogger<ProxyDispatcher> logger)
        {
            _proxySettings = proxySettings;
            _queueFactory = queueFactory;
            _logger = logger;
        }

        public void Register<T>(string queueName, Func<T, Task> handler, CancellationToken cancellation)
        {
            Task.Factory.StartNew(async () =>
            {
                using (ISubscriber subscriber = _queueFactory.CreateSubscriber(new QueueOptions(queueName, QUEUES_CAPACITY_BYTES)))
                {
                    while (!cancellation.IsCancellationRequested)
                    {
                        var result = await ReceiveAsync<T>(queueName, subscriber: subscriber);
                        if (result != null)
                        {
                            handler?.Invoke(result);
                            continue;
                        }

                        Thread.Sleep(100);
                    }
                }

                return Task.FromResult(default(T));
            });
        }

        public bool Send<T>(string queueName, T data)
        {
            EnsureProxy();

            using (IPublisher publisher = _queueFactory.CreatePublisher(new QueueOptions(queueName, QUEUES_CAPACITY_BYTES)))
            {
                var content = JsonConvert.SerializeObject(data);

                return publisher.TryEnqueue(Encoding.UTF8.GetBytes(content));
            }
        }

        public async Task<T> ReceiveAsync<T>(string queueName, int timeout = 0, ISubscriber subscriber = null)
        {
            EnsureProxy();

            bool localManaged = false;

            if (subscriber == null)
            {
                subscriber = _queueFactory.CreateSubscriber(new QueueOptions(queueName, QUEUES_CAPACITY_BYTES));
                localManaged = true;
            }

            ReadOnlyMemory<byte> contentBytes = null;
            if (timeout > 0)
            {
                await Task.Factory.StartNew(() =>
                {
                    var start = DateTime.Now;
                    while (DateTime.Now.Subtract(start).TotalSeconds < timeout)
                    {
                        if (subscriber.TryDequeue(default, out contentBytes))
                        {
                            break;
                        }

                        Thread.Sleep(50);
                    }
                });
            }
            else
            {
                subscriber.TryDequeue(default, out contentBytes);
            }

            if (localManaged)
            {
                subscriber.Dispose();
            }

            if (contentBytes.IsEmpty)
            {
                return default;
            }

            var content = Encoding.UTF8.GetString(contentBytes.ToArray());

            return JsonConvert.DeserializeObject<T>(content);
        }

        private void EnsureProxy()
        {
            var processes = Process.GetProcessesByName("WebCrawler.Proxy");
            if (processes.Length > 0)
            {
                _isProxyReady = true;
                return;
            }

            _isProxyReady = false;

            using (Process process = new Process())
            {
                process.StartInfo.FileName = _proxySettings.Path;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                process.OutputDataReceived += Process_DataReceived;
                process.ErrorDataReceived += Process_DataReceived;

                try
                {
                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    var start = DateTime.Now;
                    while (!_isProxyReady)
                    {
                        if (DateTime.Now.Subtract(start).TotalSeconds > PROXY_START_TIMEOUT_SECONDS)
                        {
                            throw new Exception("Timeout to start proxy");
                        }

                        Thread.Sleep(50);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to start proxy", ex);
                }
                finally
                {
                    process.CancelOutputRead();
                    process.CancelErrorRead();
                }
            }
        }

        private void Process_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == Constants.ProxyReadyMessage)
            {
                _isProxyReady = true;
            }
        }
    }
}
