using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WebCrawler.Housing.Models;

namespace WebCrawler.Housing.Persisters
{
    public class MySqlPersister : IPersister
    {
        private readonly HousingDbContext _dbContext;
        private readonly ILogger _logger;

        public MySqlPersister(HousingDbContext dbContext, ILogger logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SaveAsync<TModel, TKey>(TModel data, TKey key)
            where TModel : class
        {
            var model = await _dbContext.FindAsync<TModel>(key);
            if (model == null)
            {
                _dbContext.Add(data);
            }
            else
            {
                _dbContext.Entry(model).CurrentValues.SetValues(data);
            }

            await _dbContext.SaveChangesAsync();
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        #region Private Members

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

        #endregion
    }
}
