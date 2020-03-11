using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebCrawler.UI.Models;
using WebCrawler.UI.ViewModels;

namespace WebCrawler.UI.Persisters
{
    public interface IPersister : IDisposable
    {
        Task<PagedResult<Website>> GetWebsitesAsync(string keywords = null, WebsiteStatus status = WebsiteStatus.All, bool? enabled = true, bool includeLogs = false, int page = 1, string sortBy = null, bool descending = false);
        Task<PagedResult<Crawl>> GetCrawlsAsync(int page = 1);
        Task<PagedResult<CrawlLog>> GetCrawlLogsAsync(int crawlId, int? websiteId = null, string keywords = null, CrawlStatus status = CrawlStatus.All, int page = 1);
        Task SaveAsync(List<Article> articles, CrawlLogView crawlLog);
        Task SaveAsync(WebsiteView editor);
        Task<Crawl> SaveAsync(Crawl crawl = null);
        Task UpdateStatusAsync(int websiteId, WebsiteStatus status, string notes = null);
        Task ToggleAsync(bool enabled, params int[] websiteIds);
        Task DeleteAsync(params int[] websiteIds);
    }
}
