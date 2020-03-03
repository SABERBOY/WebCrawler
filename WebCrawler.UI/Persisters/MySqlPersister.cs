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
                .Include(o => o.CrawlLogs)
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

        public async Task AddAsync(List<Article> articles)
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

        public Task<PagedResult<CrawlLog>> GetCrawlLogsAsync()
        {
            var data = _dbContext.CrawlLogs.ToArray();

            return null;
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
