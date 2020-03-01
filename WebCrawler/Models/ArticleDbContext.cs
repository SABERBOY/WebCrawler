using Microsoft.EntityFrameworkCore;
using WebCrawler.Models;

namespace ArticleConsole.Models
{
    public class ArticleDbContext : DbContext
    {
        public virtual DbSet<Article> Articles { get; set; }
        public virtual DbSet<Website> Websites { get; set; }
        public virtual DbSet<WebsiteParser> WebsiteParsers { get; set; }

        public ArticleDbContext(DbContextOptions<ArticleDbContext> options)
           : base(options)
        {

        }
    }
}
