using System.Collections.Generic;
using System.Threading.Tasks;
using WebCrawler.UI.Models;
using WebCrawler.UI.ViewModels;

namespace WebCrawler.UI.Persisters
{
    public interface IPersister
    {
        Task<PagedResult<Website>> GetWebsitesAsync(string keywords = null, bool? enabled = true, int page = 1, string sortBy = null, bool descending = false);
        Task<PagedResult<CrawlLog>> GetCrawlLogsAsync(int websiteId);
        Task SaveAsync(List<Article> articles);
        Task SaveAsync(WebsiteEditor editor);
        Task UpdateStatusAsync(int websiteId, WebsiteStatus status, string notes = null);
        Task DeleteAsync(Website website);
    }
}
