using System.Threading.Tasks;

namespace WebCrawler.Housing.Crawlers
{
    public interface ICrawler
    {
        Task ExecuteAsync();
    }
}
