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
        Task<List<Website>> GetWebsitesAsync(int[] websiteIds, bool includeLogs = false);
        Task<PagedResult<Crawl>> GetCrawlsAsync(int page = 1);
        Task<PagedResult<CrawlLog>> GetCrawlLogsAsync(int? crawlId = null, int? websiteId = null, string keywords = null, CrawlStatus status = CrawlStatus.All, int page = 1);
        Task<PagedResult<Website>> GetWebsiteAnalysisQueueAsync(bool isFull = false, int? lastId = null);
        Task<PagedResult<CrawlLog>> GetCrawlingQueueAsync(int crawlId, int? lastId = null);
        Task SaveAsync(List<Article> articles, CrawlLogView crawlLog, string lastHandled);
        Task SaveAsync(WebsiteView editor);
        Task<Crawl> SaveAsync(Crawl crawl = null);
        Task UpdateStatusAsync(int websiteId, WebsiteStatus? status = null, string notes = null);
        Task ToggleAsync(bool enabled, params int[] websiteIds);
        Task DeleteAsync(params int[] websiteIds);
        Task<Crawl> QueueCrawlAsync();
        Task<Crawl> ContinueCrawlAsync(int crawlId);
    }
}
