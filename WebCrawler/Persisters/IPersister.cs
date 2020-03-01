using System.Collections.Generic;
using System.Threading.Tasks;
using WebCrawler.Models;

namespace WebCrawler.Persisters
{
    public interface IPersister
    {
        Task<List<WebsiteParser>> GetConfigsAsync();
        Task AddAsync(List<Article> articles);
    }
}
