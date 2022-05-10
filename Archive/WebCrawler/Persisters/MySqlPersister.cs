using ArticleConsole.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebCrawler.Models;

namespace WebCrawler.Persisters
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

        public async Task<List<WebsiteParser>> GetConfigsAsync()
        {
            return await _dbContext.WebsiteParsers
                .Where(o => o.Website.Enabled)
                .Include(o => o.Website)
                .OrderBy(o => o.Website.Rank)
                .ToListAsync();
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
    }
}
