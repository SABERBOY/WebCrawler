using Microsoft.EntityFrameworkCore;

namespace ArticleConsole.Models
{
    public class ArticleDbContext : DbContext
    {
        public virtual DbSet<Article> Articles { get; set; }
        public virtual DbSet<ArticleZH> ArticlesZH { get; set; }

        public ArticleDbContext(DbContextOptions<ArticleDbContext> options)
           : base(options)
        {

        }
    }
}
