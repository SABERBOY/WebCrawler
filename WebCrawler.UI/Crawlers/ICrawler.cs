using System.Threading.Tasks;

namespace WebCrawler.UI.Crawlers
{
    public interface ICrawler
    {
        Task ExecuteAsync();
    }
}
