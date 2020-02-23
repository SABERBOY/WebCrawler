using ArticleConsole.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArticleConsole.Persisters
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

        public async Task<Article> GetPreviousAsync(ArticleSource source)
        {
            return await _dbContext.Articles
                .Where(o => o.Source == source)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Article>> GetUnTranslatedAsync()
        {
            return await _dbContext.Articles
                .Where(o => !o.Translated)
                .OrderBy(o => o.Id)
                .ToListAsync();
        }

        public async Task PersistAsync(List<Article> articles, ArticleSource source)
        {
            var count = articles.Count;

            Article article;
            for (var i = count - 1; i >= 0; i--)
            {
                article = articles[i];

                article.Timestamp = DateTime.Now;

                _dbContext.Articles.Add(article);

                // batch submit
                if ((count - i) % BATCH_SIZE == 0 || i == 0)
                {
                    await _dbContext.SaveChangesAsync();

                    _logger.LogDebug("Persisting {0} feed articles: {1}/{2}", source, count - i, count);
                }
            }

            _logger.LogInformation("Persisted {0} feed: {1} articles", source, count);
        }

        public async Task PersistAsync(ArticleZH article)
        {
            _dbContext.Add(article);

            await _dbContext.SaveChangesAsync();
        }
    }
}
