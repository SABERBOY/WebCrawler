using System.Threading.Tasks;

namespace WebCrawler.Crawlers
{
    public interface ICrawler
    {
        Task ExecuteAsync();
    }
}
