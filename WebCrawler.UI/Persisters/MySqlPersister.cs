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

        public async Task<PagedResult<Website>> GetWebsitesAsync(string keywords = null, bool enabled = true, int page = 1, string sortBy = null, bool descending = false)
        {
            var query = _dbContext.Websites
                .AsNoTracking()
                .Where(o => (string.IsNullOrEmpty(keywords) || o.Name.Contains(keywords) || o.Home.Contains(keywords))
                    && o.Enabled == enabled
                );

            if (string.IsNullOrEmpty(sortBy))
            {
                query = query.OrderByDescending(o => o.Rank);
            }
            else
            {
                if (sortBy == nameof(Website.Rank))
                {
                    query = Sort<int>(query, sortBy, descending);
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

        public async Task<PagedResult<CrawlLog>> GetCrawlLogsAsync(int websiteId)
        {
            return await _dbContext.CrawlLogs
                .Where(o => o.WebsiteId == websiteId)
                .OrderByDescending(o => o.Id)
                .ToPagedResultAsync(1);
        }

        public async Task SaveAsync(List<Article> articles)
        {
            var count = articles.Count;

            Article article;
            // save from the last one in case failure
            for (var i = count - 1; i >= 0; i--)
            {
                article = articles[i];

                article.Timestamp = DateTime.Now;

                _dbContext.Articles.Add(article);

                if ((count - i) % BATCH_SIZE == 0 || i == 0)
                {
                    // record order isn't guaranteed in batch inset, so let's save the records one by one
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Persisting {0} feed articles: {1}/{2}", article.WebsiteId, i + 1, count);
                }
            }
        }

        public async Task SaveAsync(WebsiteEditor website)
        {
            if (website.Id > 0)
            {
                var model = await _dbContext.Websites.FindAsync(website.Id);

                model.Name = website.Name;
                model.Rank = website.Rank;
                model.Home = website.Home;
                model.UrlFormat = website.UrlFormat;
                model.StartIndex = website.StartIndex;
                model.ListPath = website.ListPath;
                model.Notes = website.Notes;
                model.Enabled = website.Enabled;
            }
            else
            {
                _dbContext.Websites.Add(new Website
                {
                    Name = website.Name,
                    Enabled = website.Enabled,
                    Home = website.Home,
                    UrlFormat = website.UrlFormat,
                    StartIndex = website.StartIndex,
                    ListPath = website.ListPath,
                    Rank = website.Rank,
                    Notes = website.Notes,
                    Registered = DateTime.Now
                });
            }

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
