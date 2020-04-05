using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebCrawler.Core;
using WebCrawler.UI.Common;
using WebCrawler.UI.Models;
using WebCrawler.UI.ViewModels;

namespace WebCrawler.UI.Persisters
{
    public class MySqlPersister : IPersister
    {
        private readonly ArticleDbContext _dbContext;
        private readonly ILogger _logger;

        public MySqlPersister(ArticleDbContext dbContext, ILogger logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<PagedResult<Website>> GetWebsitesAsync(string keywords = null, WebsiteStatus status = WebsiteStatus.All, bool? enabled = true, bool includeLogs = false, int page = 1, string sortBy = null, bool descending = false)
        {
            var query = _dbContext.Websites
                .AsNoTracking()
                .Where(o => (enabled == null || o.Enabled == enabled)
                    && (string.IsNullOrEmpty(keywords) || o.Name.Contains(keywords) || o.Home.Contains(keywords) || o.Notes.Contains(keywords) || o.SysNotes.Contains(keywords))
                    && (status == WebsiteStatus.All || o.Status == status)
                );

            if (includeLogs)
            {
                query = query.Include(o => o.CrawlLogs);
            }

            if (string.IsNullOrEmpty(sortBy))
            {
                query = query.OrderByDescending(o => o.Id);
            }
            else
            {
                // TODO: use reflection to get the proper data types
                if (sortBy == nameof(Website.Rank) || sortBy == nameof(Website.Id))
                {
                    query = Sort<int>(query, sortBy, descending);
                }
                else if (sortBy == nameof(Website.Status))
                {
                    query = Sort<WebsiteStatus>(query, sortBy, descending);
                }
                else if (sortBy == nameof(Website.Registered))
                {
                    query = Sort<DateTime>(query, sortBy, descending);
                }
                else
                {
                    query = Sort<string>(query, sortBy, descending);
                }
            }

            return await query.ToPagedResultAsync(page);
        }

        public async Task<List<Website>> GetWebsitesAsync(int[] websiteIds, bool includeLogs = false)
        {
            var query = _dbContext.Websites
               .AsNoTracking()
               .Where(o => websiteIds.Contains(o.Id));

            if (includeLogs)
            {
                query = query.Include(o => o.CrawlLogs);
            }

            return await query.ToListAsync();
        }

        public async Task<PagedResult<Crawl>> GetCrawlsAsync(int page = 1)
        {
            return await _dbContext.Crawls
                .AsNoTracking()
                .OrderByDescending(o => o.Id)
                .ToPagedResultAsync(page);
        }

        public async Task<PagedResult<CrawlLog>> GetCrawlLogsAsync(int? crawlId = null, int? websiteId = null, string keywords = null, CrawlStatus status = CrawlStatus.All, int page = 1)
        {
            return await _dbContext.CrawlLogs
                .AsNoTracking()
                .Include(o => o.Website)
                .Where(o => (crawlId == null || o.CrawlId == crawlId)
                    && (websiteId == null || o.WebsiteId == websiteId)
                    && (string.IsNullOrEmpty(keywords) || o.Website.Name.Contains(keywords) || o.Website.Home.Contains(keywords))
                    && (status == CrawlStatus.All || o.Status == status)
                )
                .OrderByDescending(o => o.Crawled)
                .ThenByDescending(o => o.Id)
                .ToPagedResultAsync(page);
        }

        public async Task<PagedResult<Website>> GetWebsiteAnalysisQueueAsync(bool isFull = false, int? lastId = null)
        {
            return await _dbContext.Websites
                .AsNoTracking()
                .Where(o => (isFull || o.Enabled)
                    && (lastId == null || o.Id < lastId)
                    //&& o.Status != WebsiteStatus.ErrorBroken
                )
                .OrderByDescending(o => o.Id)
                .ToPagedResultAsync(1);
        }

        public async Task<PagedResult<CrawlLog>> GetCrawlingQueueAsync(int crawlId, int? lastId = null)
        {
            return await _dbContext.CrawlLogs
                .AsNoTracking()
                .Include(o => o.Website)
                .Where(o => o.CrawlId == crawlId
                    && o.Status != CrawlStatus.Completed
                    && (lastId == null || o.Id < lastId)
                )
                .OrderByDescending(o => o.Id)
                .ToPagedResultAsync(1);
        }

        public async Task<T> GetAsync<T>(int id)
            where T : class
        {
            return await _dbContext.FindAsync<T>(id);
        }

        public async Task SaveAsync(List<Article> articles, CrawlLogView crawlLogView, string lastHandled)
        {
            if (crawlLogView.Status != CrawlStatus.Failed)
            {
                crawlLogView.Status = CrawlStatus.Committing;

                foreach (var article in articles)
                {
                    // Escape the URL as it will be stored in ASCII as Unique doesn't accept text longer than 255 chars in UTF8.
                    article.Url = Uri.EscapeUriString(article.Url);

                    _dbContext.Articles.Add(article);

                    try
                    {
                        // TODO: consider to create separated commits based on the content length
                        // commit article one by one as some articles might be really large, e.g. the following which involves BASE64 image data
                        // http://d.drcnet.com.cn/eDRCnet.common.web/DocDetail.aspx?chnid=1012&leafid=5&docid=5738629&uid=030201&version=integrated
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        if (Regex.IsMatch((ex.InnerException ?? ex).Message, "Duplicate entry '.+' for key 'url_UNIQUE'"))
                        {
                            // skip silently as article already exists
                            _dbContext.Entry(article).State = EntityState.Detached;
                            continue;
                        }

                        throw;
                    }
                }
            }

            var crawlLogModel = await _dbContext.CrawlLogs.FindAsync(crawlLogView.Id);

            crawlLogModel.Success = crawlLogView.Success;
            crawlLogModel.Fail = crawlLogView.Fail;
            crawlLogModel.Status = crawlLogView.Status == CrawlStatus.Failed ? CrawlStatus.Failed : CrawlStatus.Completed;
            crawlLogModel.Notes = crawlLogView.Notes;
            crawlLogModel.Crawled = crawlLogView.Crawled;
            if (!string.IsNullOrEmpty(lastHandled))
            {
                crawlLogModel.LastHandled = lastHandled;
            }

            await _dbContext.SaveChangesAsync();

            crawlLogView.Status = crawlLogModel.Status;
            crawlLogView.LastHandled = crawlLogModel.LastHandled;
        }

        public async Task SaveAsync(WebsiteView editor)
        {
            Website model;
            if (editor.Id > 0)
            {
                model = await _dbContext.Websites.FindAsync(editor.Id);

                model.Name = editor.Name;
                model.Rank = editor.Rank;
                model.Home = editor.Home;
                model.UrlFormat = editor.UrlFormat;
                model.StartIndex = editor.StartIndex;
                model.ListPath = editor.ListPath;
                model.Notes = editor.Notes;
                model.SysNotes = editor.SysNotes;
                model.Enabled = editor.Enabled;
                model.Status = editor.Status;
            }
            else
            {
                model = new Website
                {
                    Name = editor.Name,
                    Enabled = editor.Enabled,
                    Home = editor.Home,
                    UrlFormat = editor.UrlFormat,
                    StartIndex = editor.StartIndex,
                    ListPath = editor.ListPath,
                    Rank = editor.Rank,
                    Notes = editor.Notes,
                    Status = editor.Status,
                    SysNotes = null,
                    Registered = DateTime.Now
                };
                _dbContext.Websites.Add(model);
            }

            await _dbContext.SaveChangesAsync();

            editor.Id = model.Id;
            editor.Registered = model.Registered;
        }

        public async Task<Crawl> SaveAsync(Crawl crawl = null)
        {
            Crawl model;
            if (crawl?.Id > 0)
            {
                model = await _dbContext.Crawls.FindAsync(crawl.Id);

                _dbContext.Entry(model).CurrentValues.SetValues(crawl);
            }
            else
            {
                model = new Crawl
                {
                    Started = DateTime.Now
                };
                _dbContext.Crawls.Add(model);

                await _dbContext.SaveChangesAsync();
            }

            return model;
        }

        /// <summary>
        /// Update website status, and disalbe it automatically if the status isn't Normal.
        /// </summary>
        /// <param name="websiteId"></param>
        /// <param name="status"></param>
        /// <param name="notes"></param>
        /// <returns></returns>
        public async Task UpdateStatusAsync(int websiteId, WebsiteStatus? status = null, string notes = null)
        {
            var model = await _dbContext.Websites.FindAsync(websiteId);

            if (status != null)
            {
                var enabled = WebsiteView.DetermineWebsiteEnabledStatus(status.Value, model.Status);
                if (enabled != null)
                {
                    model.Enabled = enabled.Value;
                }

                model.Status = status.Value;
            }

            model.SysNotes = notes;

            await _dbContext.SaveChangesAsync();
        }

        public async Task ToggleAsync(bool enabled, params int[] websiteIds)
        {
            var models = await _dbContext.Websites.Where(o => websiteIds.Contains(o.Id)).ToArrayAsync();

            models.ForEach(o => o.Enabled = enabled);

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(params int[] websiteIds)
        {
            var models = await _dbContext.Websites.Where(o => websiteIds.Contains(o.Id)).ToArrayAsync();

            _dbContext.Websites.RemoveRange(models);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<Crawl> QueueCrawlAsync()
        {
            var crawl = new Crawl
            {
                Status = CrawlStatus.Queued,
                Started = DateTime.Now,
            };

            _dbContext.Crawls.Add(crawl);
            await _dbContext.SaveChangesAsync();

            var sql = @"INSERT INTO atc_crawllogs (websiteid, crawlid, status, lasthandled)
	            SELECT WS.id, {0}, 'QUEUED', (
			        SELECT CL.lasthandled FROM atc_crawllogs AS CL WHERE CL.websiteid = WS.id ORDER BY id DESC LIMIT 1
		        )
                FROM atc_websites AS WS
		        WHERE enabled = 1";

            await ExecuteSqlAsync(sql, crawl.Id);

            return crawl;
        }

        public async Task<Crawl> ContinueCrawlAsync(int crawlId)
        {
            var sql = @"UPDATE atc_crawllogs SET status = 'QUEUED', success = 0, fail = 0, notes = NULL WHERE crawlid = {0} AND status IN ('FAILED', 'CANCELLED')";

            await ExecuteSqlAsync(sql, crawlId);

            var crawl = await _dbContext.Crawls.FindAsync(crawlId);

            crawl.Status = CrawlStatus.Queued;

            await _dbContext.SaveChangesAsync();

            return crawl;
        }

        private async Task ExecuteSqlAsync(string sql, params object[] parameters)
        {
            using (var tran = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    await _dbContext.Database.ExecuteSqlRawAsync(sql, parameters);

                    await tran.CommitAsync();
                }
                catch (Exception)
                {
                    await tran.RollbackAsync();

                    throw;
                }
            }
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        #region Private Members

        private IOrderedQueryable<Website> Sort<T>(IQueryable<Website> query, string property, bool descending)
        {
            var sortKeySelector = LinqHelper.CreateKeyAccessor<Website, T>(property);
            if (descending)
            {
                return query.OrderByDescending(sortKeySelector);
            }
            else
            {
                return query.OrderBy(sortKeySelector);
            }
        }

        #endregion
    }
}
