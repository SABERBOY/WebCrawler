using System.Collections.Generic;
using System.Threading.Tasks;
using WebCrawler.UI.Models;

namespace WebCrawler.UI.Persisters
{
    public interface IPersister
    {
        Task<List<Website>> GetActiveConfigsAsync();
        Task AddAsync(List<Article> articles);
    }
}
