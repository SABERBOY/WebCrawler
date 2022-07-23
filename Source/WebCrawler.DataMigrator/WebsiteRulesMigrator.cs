using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebCrawler.Models;

namespace WebCrawler.DataMigrator
{
    public class WebsiteRulesMigrator
    {
        private readonly ArticleDbContext _dbContext;
        private readonly ILogger _logger;

        public WebsiteRulesMigrator(ArticleDbContext dbContext, ILogger<WebsiteRulesMigrator> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var websites = await _dbContext.Websites
                .Where(o => !string.IsNullOrEmpty(o.ListPath))
                .ToListAsync();

            foreach (var website in websites)
            {
                _dbContext.WebsiteRules.Add(new WebsiteRule
                {
                    RuleId = Guid.NewGuid(),
                    Type = WebsiteRuleType.Catalog,
                    WebsiteId = website.Id,
                    PageLoadOption = PageLoadOption.Default,
                    PageUrlReplacement = website.DataUrl,
                    ContentMatchType = website.ListMatchType,
                    ContentUrlExp = website.ListPath
                });
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
