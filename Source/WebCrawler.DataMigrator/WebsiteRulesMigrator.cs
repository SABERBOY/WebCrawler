using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebCrawler.Models;

namespace WebCrawler.DataMigrator
{
    public class WebsiteRulesMigrator
    {
        private readonly ArticleDbContext _dbContext;
        private readonly ArticleDbContextPG _dbContextPG;
        private readonly ILogger _logger;

        public WebsiteRulesMigrator(ArticleDbContext dbContext, ArticleDbContextPG dbContextPG, ILogger<WebsiteRulesMigrator> logger)
        {
            _dbContext = dbContext;
            _dbContextPG = dbContextPG;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var websitePGs = await _dbContextPG.Websites
                .Include(o => o.Rules)
                .OrderBy(o => o.Id)
                .ToListAsync();

            foreach (var wpg in websitePGs)
            {
                wpg.Id = 0;
                wpg?.Rules.ForEach(o =>
                {
                    o.WebsiteId = 0;
                    o.Website = null;
                });

                _dbContext.Websites.Add(wpg);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
