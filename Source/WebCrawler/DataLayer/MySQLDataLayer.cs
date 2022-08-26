using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using WebCrawler.Common;
using WebCrawler.DTO;
using WebCrawler.Models;

namespace WebCrawler.DataLayer
{
    public class MySQLDataLayer : IDataLayer
    {
        private readonly ArticleDbContext _dbContext;
        private readonly ILogger _logger;

        public MySQLDataLayer(ArticleDbContext dbContext, ILogger<MySQLDataLayer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<PagedResult<WebsiteDTO>> GetWebsitesAsync(string keywords = null, WebsiteStatus status = WebsiteStatus.All, bool? enabled = true, bool includeLogs = false, int page = 1, string sortBy = null, bool descending = false)
        {
            var query = _dbContext.Websites
                .AsNoTracking()
                .Include(o => o.Rules)
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

            return await query
                .Select(o => new WebsiteDTO(o))
                .ToPagedResultAsync(page);
        }

        public async Task<List<WebsiteDTO>> GetWebsitesAsync(int[] websiteIds, bool includeLogs = false)
        {
            var query = _dbContext.Websites
               .AsNoTracking()
               .Include(o => o.Rules)
               .Where(o => websiteIds.Contains(o.Id));

            if (includeLogs)
            {
                query = query.Include(o => o.CrawlLogs);
            }

            return await query
                .Select(o => new WebsiteDTO(o))
                .ToListAsync();
        }

        public async Task<PagedResult<CrawlDTO>> GetCrawlsAsync(int page = 1)
        {
            return await _dbContext.Crawls
                .AsNoTracking()
                .OrderByDescending(o => o.Id)
                .Select(o => new CrawlDTO(o))
                .ToPagedResultAsync(page);
        }

        public async Task<PagedResult<CrawlLogDTO>> GetCrawlLogsAsync(int? crawlId = null, int? websiteId = null, string keywords = null, CrawlStatus status = CrawlStatus.All, int page = 1)
        {
            return await _dbContext.CrawlLogs
                .AsNoTracking()
                .Include(o => o.Website.Rules)
                .Where(o => (crawlId == null || o.CrawlId == crawlId)
                    && (websiteId == null || o.WebsiteId == websiteId)
                    && (string.IsNullOrEmpty(keywords) || o.Website.Name.Contains(keywords) || o.Website.Home.Contains(keywords))
                    && (status == CrawlStatus.All || o.Status == status)
                )
                .OrderByDescending(o => o.Crawled)
                .ThenByDescending(o => o.Id)
                .Select(o => new CrawlLogDTO(o))
                .ToPagedResultAsync(page);
        }

        public async Task<PagedResult<WebsiteDTO>> GetWebsiteAnalysisQueueAsync(bool isFull = false, int? lastId = null)
        {
            return await _dbContext.Websites
                .AsNoTracking()
                .Include(o => o.Rules)
                .Where(o => (isFull || o.Enabled)
                    && (lastId == null || o.Id < lastId)
                    //&& o.Status != WebsiteStatus.ErrorBroken
                )
                .OrderByDescending(o => o.Id)
                .Select(o => new WebsiteDTO(o))
                .ToPagedResultAsync(1);
        }

        public async Task<PagedResult<CrawlLogDTO>> GetCrawlingQueueAsync(int crawlId, int? lastId = null)
        {
            return await _dbContext.CrawlLogs
                .AsNoTracking()
                .Include(o => o.Website.Rules)
                .Where(o => o.CrawlId == crawlId
                    && o.Status != CrawlStatus.Completed
                    && (lastId == null || o.Id < lastId)
                )
                .OrderByDescending(o => o.Id)
                .Select(o => new CrawlLogDTO(o))
                .ToPagedResultAsync(1);
        }

        public async Task<T> GetAsync<T>(int id)
            where T : class
        {
            return await _dbContext.FindAsync<T>(id);
        }

        public async Task SaveAsync(List<Article> articles, CrawlLogDTO crawlLog, string lastHandled)
        {
            if (crawlLog.Status != CrawlStatus.Failed)
            {
                crawlLog.Status = CrawlStatus.Committing;

                foreach (var article in articles)
                {
                    // Escape the URL as it will be stored in ASCII as Unique (in MySQL) doesn't accept text longer than 255 chars in UTF8.
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
                        if (Regex.IsMatch(ex.GetBaseException().Message, @"Duplicate entry .+ for key .+\Wurl_UNIQUE"))
                        {
                            // skip silently as article already exists, this might happen if multiple website instances contain the same article
                            _dbContext.Entry(article).State = EntityState.Detached;
                            continue;
                        }

                        throw;
                    }
                }
            }

            var model = await _dbContext.CrawlLogs.FindAsync(crawlLog.Id);

            model.Success = crawlLog.Success;
            model.Fail = crawlLog.Fail;
            model.Status = crawlLog.Status == CrawlStatus.Failed ? CrawlStatus.Failed : CrawlStatus.Completed;
            model.Notes = crawlLog.Notes;
            model.Crawled = crawlLog.Crawled;
            if (!string.IsNullOrEmpty(lastHandled))
            {
                model.LastHandled = lastHandled;
            }

            // track/untrack broken websites
            var website = await _dbContext.Websites.FindAsync(crawlLog.WebsiteId);
            website.Enabled = crawlLog.Website.Enabled;
            website.Status = crawlLog.Website.Status;
            website.BrokenSince = crawlLog.Website.BrokenSince;
            website.SysNotes = crawlLog.Website.SysNotes;

            await _dbContext.SaveChangesAsync();

            crawlLog.Status = model.Status;
            crawlLog.LastHandled = model.LastHandled;
        }

        public async Task SaveAsync(WebsiteDTO website)
        {
            Website model;
            if (website.Id > 0)
            {
                model = await _dbContext.Websites
                    .Include(o => o.Rules)
                    .SingleOrDefaultAsync(o => o.Id == website.Id);

                model.Name = website.Name;
                model.Rank = website.Rank;
                model.Home = website.Home;
                model.UrlFormat = website.UrlFormat;
                model.StartIndex = website.StartIndex;
                model.Notes = website.Notes;
                model.SysNotes = website.SysNotes;
                model.BrokenSince = website.BrokenSince;
                model.Enabled = website.Enabled;
                model.Status = website.Status;

                var removeRules = model.Rules.Where(o => !website.Rules.Any(r => r.RuleId == o.RuleId)).ToArray();
                var addRules = website.Rules.Where(o => !model.Rules.Any(r => r.RuleId == o.RuleId)).ToArray();
                var updateRules = website.Rules.Except(addRules).ToArray();

                _dbContext.WebsiteRules.RemoveRange(removeRules);
                // NOTES: entity adding in PostgreSQL via the navigation properties appears not working, we should use the dbcontext properties instead
                //model.Rules.AddRange(addRules.Select(o => o.CloneTo((WebsiteRule)null)));
                _dbContext.WebsiteRules.AddRange(addRules.Select(o => o.CloneTo((WebsiteRule)null)));
                foreach (var rule in updateRules)
                {
                    var ruleModel = model.Rules.SingleOrDefault(o => o.RuleId == rule.RuleId);

                    rule.CloneTo(ruleModel);
                }
            }
            else
            {
                model = new Website
                {
                    Name = website.Name,
                    Enabled = website.Enabled,
                    Home = website.Home,
                    UrlFormat = website.UrlFormat,
                    StartIndex = website.StartIndex,
                    Rank = website.Rank,
                    Notes = website.Notes,
                    Status = website.Status,
                    SysNotes = null,
                    Registered = DateTime.Now,
                    Rules = website.Rules.Select(o => o.CloneTo((WebsiteRule)null)).ToList()
                };
                _dbContext.Websites.Add(model);
            }

            await _dbContext.SaveChangesAsync();

            website.Id = model.Id;
            website.Registered = model.Registered;
        }

        public async Task<CrawlDTO> SaveAsync(CrawlDTO crawl = null)
        {
            Crawl model;
            if (crawl?.Id > 0)
            {
                model = await _dbContext.Crawls.FindAsync(crawl.Id);

                //_dbContext.Entry(model).CurrentValues.SetValues(crawl);
                crawl.CloneTo(model);
            }
            else
            {
                model = new Crawl
                {
                    Started = DateTime.Now
                };
                _dbContext.Crawls.Add(model);
            }

            await _dbContext.SaveChangesAsync();

            if (crawl != null)
            {
                crawl.Id = model.Id;
                crawl.Started = DateTime.Now;
            }
            else
            {
                crawl = new CrawlDTO(model);
            }

            return crawl;
        }

        /// <summary>
        /// Update website status, and disalbe it automatically if the status isn't Normal.
        /// </summary>
        /// <param name="websiteId"></param>
        /// <param name="status"></param>
        /// <param name="notes"></param>
        /// <returns></returns>
        public async Task UpdateStatusAsync(int websiteId, WebsiteStatus? status = null, bool? enabled = null, string notes = null)
        {
            var model = await _dbContext.Websites.FindAsync(websiteId);

            if (status != null)
            {                
                model.Status = status.Value;
            }

            if (enabled != null)
            {
                model.Enabled = enabled.Value;
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
            //var rules = await _dbContext.WebsiteRules.Where(o => websiteIds.Contains(o.WebsiteId)).ToArrayAsync();
            var websites = await _dbContext.Websites.Where(o => websiteIds.Contains(o.Id)).ToArrayAsync();

            //_dbContext.WebsiteRules.RemoveRange(rules);
            _dbContext.Websites.RemoveRange(websites);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<WebsiteDTO[]> DuplicateAsync(params int[] websiteIds)
        {
            var models = await _dbContext.Websites
                .Include(o => o.Rules)
                .Where(o => websiteIds.Contains(o.Id))
                .ToArrayAsync();

            var duplicates = models.Select(o => new Website
            {
                Name = o.Name,
                // disable the new websites by default
                Enabled = false,
                Home = o.Home,
                UrlFormat = o.UrlFormat,
                StartIndex = o.StartIndex,
                Rank = o.Rank,
                Notes = o.Notes,
                Registered = DateTime.Now,
                Rules = o.Rules.Select(r => new WebsiteRule
                {
                    RuleId = Guid.NewGuid(),
                    Type = r.Type,
                    PageLoadOption = r.PageLoadOption,
                    PageUrlReviseExp = r.PageUrlReviseExp,
                    PageUrlReplacement = r.PageUrlReplacement,
                    ContentMatchType = r.ContentMatchType,
                    ContentRootExp = r.ContentRootExp,
                    ContentUrlExp = r.ContentUrlExp,
                    ContentUrlReviseExp = r.ContentUrlReviseExp,
                    ContentUrlReplacement = r.ContentUrlReplacement,
                    ContentTitleExp = r.ContentTitleExp,
                    ContentDateExp = r.ContentDateExp,
                    ContentExp = r.ContentExp
                })
                .ToList()
            });

            _dbContext.Websites.AddRange(duplicates);

            await _dbContext.SaveChangesAsync();

            return duplicates.Select(o => new WebsiteDTO(o)).ToArray();
        }

        public async Task<CrawlDTO> QueueCrawlAsync()
        {
            var crawl = new Crawl
            {
                Status = CrawlStatus.Queued,
                Started = DateTime.Now,
            };

            _dbContext.Crawls.Add(crawl);
            await _dbContext.SaveChangesAsync();

            // 直接在数据库中进行大批量数据操作
            var sql = @"INSERT INTO atc_crawllogs (websiteid, crawlid, status, success, fail, lasthandled)
	            SELECT WS.id, {0}, 'QUEUED', 0, 0, (
			        SELECT CL.lasthandled FROM atc_crawllogs AS CL WHERE CL.websiteid = WS.id ORDER BY id DESC LIMIT 1
		        )
                FROM atc_websites AS WS
		        WHERE enabled";

            await ExecuteSqlAsync(sql, crawl.Id);

            return new CrawlDTO(crawl);
        }

        public async Task<CrawlDTO> ContinueCrawlAsync(int crawlId)
        {
            // 直接在数据库中进行大批量数据操作
            var sql = @"UPDATE atc_crawllogs SET status = 'QUEUED', success = 0, fail = 0, notes = NULL WHERE crawlid = {0} AND status IN ('FAILED', 'CANCELLED')";

            await ExecuteSqlAsync(sql, crawlId);

            var crawl = await _dbContext.Crawls.FindAsync(crawlId);

            crawl.Status = CrawlStatus.Queued;

            await _dbContext.SaveChangesAsync();

            return new CrawlDTO(crawl);
        }

        private async Task ExecuteSqlAsync(string sql, params object[] parameters)
        {
            // TODO
            // https://www.nuget.org/packages/Npgsql
            /*
             // Insert some data
await using (var cmd = new NpgsqlCommand("INSERT INTO data (some_field) VALUES (@p)", conn))
{
    cmd.Parameters.AddWithValue("p", "Hello world");
    await cmd.ExecuteNonQueryAsync();
}

// Retrieve all rows
await using (var cmd = new NpgsqlCommand("SELECT some_field FROM data", conn))
await using (var reader = await cmd.ExecuteReaderAsync())
{
while (await reader.ReadAsync())
    Console.WriteLine(reader.GetString(0));
}
             */
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
