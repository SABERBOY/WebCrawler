using WebCrawler.Common;
using WebCrawler.DTO;
using WebCrawler.Models;

namespace WebCrawler.DataLayer
{
    public interface IDataLayer : IDisposable
    {
        Task<PagedResult<WebsiteDTO>> GetWebsitesAsync(WebsiteView view, string keywords = null, WebsiteStatus status = WebsiteStatus.All, bool? enabled = true, bool includeLogs = false, int page = 1, string sortBy = null, bool descending = false);
        Task<List<WebsiteDTO>> GetWebsitesAsync(int[] websiteIds, bool includeLogs = false);
        Task<PagedResult<CrawlDTO>> GetCrawlsAsync(int page = 1);
        Task<PagedResult<CrawlLogDTO>> GetCrawlLogsAsync(int? crawlId = null, int? websiteId = null, string keywords = null, CrawlStatus status = CrawlStatus.All, int page = 1);
        Task<PagedResult<WebsiteDTO>> GetWebsiteAnalysisQueueAsync(bool isFull = false, int? lastId = null);
        Task<PagedResult<CrawlLogDTO>> GetCrawlingQueueAsync(int crawlId, int? lastId = null);
        Task<T> GetAsync<T>(int id) where T : class;
        Task SaveAsync(List<Article> articles, CrawlLogDTO crawlLog, string lastHandled);
        Task SaveAsync(WebsiteDTO editor);
        Task<CrawlDTO> SaveAsync(CrawlDTO crawl = null);
        Task UpdateStatusAsync(int websiteId, WebsiteStatus? status = null, bool? enabled = null, string notes = null);
        Task ToggleAsync(bool enabled, params int[] websiteIds);
        Task DeleteAsync(params int[] websiteIds);
        Task<WebsiteDTO[]> DuplicateAsync(params int[] websiteIds);
        Task<CrawlDTO> QueueCrawlAsync();
        Task<CrawlDTO> ContinueCrawlAsync(int crawlId);
    }
}
