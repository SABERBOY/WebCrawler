using Microsoft.EntityFrameworkCore;

namespace WebCrawler.Models
{
    public class ArticleDbContextPG : DbContext
    {
        public virtual DbSet<Article> Articles { get; set; }
        public virtual DbSet<Website> Websites { get; set; }
        public virtual DbSet<WebsiteRule> WebsiteRules { get; set; }
        public virtual DbSet<Crawl> Crawls { get; set; }
        public virtual DbSet<CrawlLog> CrawlLogs { get; set; }

        public ArticleDbContextPG(DbContextOptions<ArticleDbContextPG> options)
           : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CrawlLog>()
                .HasOne(o => o.Website)
                .WithMany(o => o.CrawlLogs)
                .HasForeignKey(o => o.WebsiteId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CrawlLog>()
                .HasOne(o => o.Crawl)
                .WithMany()
                .HasForeignKey(o => o.CrawlId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WebsiteRule>()
                .HasOne(o => o.Website)
                .WithMany(o => o.Rules)
                .HasForeignKey(o => o.WebsiteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
