using ArticleConsole.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArticleConsole.Persisters
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

        public Article GetPrevious(ArticleSource source)
        {
            lock (_dbContext)
            {
                return _dbContext.Articles
                    .AsNoTracking()
                    .Where(o => o.Source == source)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefault();
            }
        }

        public int GetListCount(TransactionStatus status, ArticleSource? source = null)
        {
            lock (_dbContext)
            {
                return _dbContext.Articles
                    .AsNoTracking()
                    .Where(o => (source == null || o.Source == source) && o.Status == status)
                    .Count();
            }
        }

        public List<Article> GetList(TransactionStatus status, int batchSize, int? from = null, ArticleSource? source = null)
        {
            lock (_dbContext)
            {
                return _dbContext.Articles
                    .AsNoTracking()
                    .Where(o => (source == null || o.Source == source) && (from == null || o.Id < from) && o.Status == status)
                    .OrderByDescending(o => o.Id)
                    .Take(batchSize)
                    .ToList();
            }
        }

        public void Add(Article article)
        {
            lock (_dbContext)
            {
                article.Timestamp = DateTime.Now;

                _dbContext.Articles.Add(article);

                _dbContext.SaveChanges();
            }
        }

        public void Add(List<Article> articles)
        {
            int step = 100;
            var count = articles.Count;

            lock (_dbContext)
            {
                Article article;
                // save from the last one in case failure
                for (var i = count - 1; i >= 0; i--)
                {
                    article = articles[i];

                    article.Timestamp = DateTime.Now;

                    _dbContext.Articles.Add(article);

                    // record order isn't guaranteed in batch inset, so let's save the records one by one
                    _dbContext.SaveChanges();

                    if ((count - i) % step == 0 || i == 0)
                    {
                        _logger.LogInformation("[{0}] Persisting articles: {1}/{2}", article.Source, i + 1, count);
                    }
                }
            }
        }

        public void AddTranslation(ArticleZH article)
        {
            lock (_dbContext)
            {
                article.Timestamp = DateTime.Now;

                _dbContext.ArticlesZH.Add(article);

                var source = _dbContext.Articles.Find(article.Id);

                source.Status = TransactionStatus.TranslationCompleted;
                source.Timestamp = DateTime.Now;

                _dbContext.SaveChanges();
            }
        }

        public void Update(Article article)
        {
            lock (_dbContext)
            {
                article.Timestamp = DateTime.Now;

                var model = _dbContext.Articles.Find(article.Id);

                _dbContext.Entry(model).CurrentValues.SetValues(article);

                _dbContext.SaveChanges();
            }
        }
    }
}
