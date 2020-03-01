using Microsoft.EntityFrameworkCore;

namespace WebCrawler.UI.Models
{
    public class ArticleDbContext : DbContext
    {
        public virtual DbSet<Article> Articles { get; set; }
        public virtual DbSet<Website> Websites { get; set; }
        public virtual DbSet<CrawlLog> CrawlLogs { get; set; }

        public ArticleDbContext(DbContextOptions<ArticleDbContext> options)
           : base(options)
        {

        }
    }
}
