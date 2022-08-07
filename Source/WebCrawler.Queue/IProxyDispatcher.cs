using Cloudtoid.Interprocess;

namespace WebCrawler.Queue
{
    public interface IProxyDispatcher
    {
        /// <summary>
        /// Long-running monitoring the incoming data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <param name="handler"></param>
        /// <param name="cancellation"></param>
        void Register<T>(string queueName, Func<T, Task> handler, CancellationToken cancellation);

        /// <summary>
        /// Send data to queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        bool Send<T>(string queueName, T data);

        /// <summary>
        /// Reiceive data from queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <param name="timeout">Block the thread until data is received, otherwise return empty data immediately</param>
        /// <returns></returns>
        Task<T> ReceiveAsync<T>(string queueName, int timeout = 0, ISubscriber subscriber = null);
    }
}
