namespace WebCrawler.Crawlers
{
    public interface ICrawler
    {
        Task ExecuteAsync(bool continuePrevious = false);
    }
}
