using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebCrawler.Common;
using WebCrawler.UI.Common;
using WebCrawler.UI.Models;
using WebCrawler.UI.ViewModels;

namespace WebCrawler.UI.Persisters
{
    public class MySqlPersister : IPersister
    {
        private readonly static int BATCH_SIZE = 100;

        private readonly ArticleDbContext _dbContext;
        private readonly ILogger _logger;

        public MySqlPersister(ArticleDbContext dbContext, ILogger logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<PagedResult<Website>> GetWebsitesAsync(string keywords = null, WebsiteStatus? status = null, bool? enabled = true, bool includeLogs = false, int page = 1, string sortBy = null, bool descending = false)
        {
            var query = _dbContext.Websites
                .AsNoTracking()
                .Where(o => (enabled == null || o.Enabled == enabled)
                    && (string.IsNullOrEmpty(keywords) || o.Name.Contains(keywords) || o.Home.Contains(keywords) || o.Notes.Contains(keywords) || o.SysNotes.Contains(keywords))
                    && (status == null || o.Status == status)
                );

            if (includeLogs)
            {
                query = query.Include(o => o.CrawlLogs);
            }

            if (string.IsNullOrEmpty(sortBy))
            {
                query = query.OrderByDescending(o => o.Rank);
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

        public async Task<PagedResult<Crawl>> GetCrawlsAsync(int page = 1)
        {
            return await _dbContext.Crawls
                .AsNoTracking()
                .OrderByDescending(o => o.Id)
                .ToPagedResultAsync(page);
        }

        public async Task<PagedResult<CrawlLog>> GetCrawlLogsAsync(int crawlId, int? websiteId = null, string keywords = null, CrawlStatus? status = null, int page = 1)
        {
            return await _dbContext.CrawlLogs
                .AsNoTracking()
                .Include(o => o.Website)
                .Where(o => o.CrawlId == crawlId
                    && (websiteId == null || o.WebsiteId == websiteId)
                    && (string.IsNullOrEmpty(keywords) || o.Website.Name.Contains(keywords) || o.Website.Home.Contains(keywords))
                    && (status == null || o.Status == status)
                )
                .OrderByDescending(o => o.Id)
                .ToPagedResultAsync(1);
        }

        public async Task SaveAsync(List<Article> articles, CrawlLog crawlLog)
        {
            _dbContext.Articles.AddRange(articles);
            _dbContext.CrawlLogs.Add(crawlLog);

            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveAsync(WebsiteEditor editor)
        {
            if (editor.Id > 0)
            {
                var model = await _dbContext.Websites.FindAsync(editor.Id);

                model.Name = editor.Name;
                model.Rank = editor.Rank;
                model.Home = editor.Home;
                model.UrlFormat = editor.UrlFormat;
                model.StartIndex = editor.StartIndex;
                model.ListPath = editor.ListPath;
                model.Notes = editor.Notes;
                model.Enabled = editor.Enabled;
                model.Status = editor.Status;
            }
            else
            {
                _dbContext.Websites.Add(new Website
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
                });
            }

            await _dbContext.SaveChangesAsync();
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
        public async Task UpdateStatusAsync(int websiteId, WebsiteStatus status, string notes = null)
        {
            var model = await _dbContext.Websites.FindAsync(websiteId);

            model.Status = status;
            model.SysNotes = notes;

            if (status != WebsiteStatus.Normal && status != WebsiteStatus.WarningNoDates)
            {
                model.Enabled = false;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task ToggleAsync(Website[] websites, bool enabled)
        {
            var websiteIds = websites.Select(o => o.Id).ToArray();

            var models = await _dbContext.Websites.Where(o => websiteIds.Contains(o.Id)).ToArrayAsync();

            models.ForEach(o => o.Enabled = enabled);

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Website website)
        {
            var model = await _dbContext.Websites.FindAsync(website.Id);
            if (model != null)
            {
                _dbContext.Websites.Remove(model);
                await _dbContext.SaveChangesAsync();
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
