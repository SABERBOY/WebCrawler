using Microsoft.EntityFrameworkCore;

namespace WebCrawler.Housing.Models
{
    public class HousingDbContext : DbContext
    {
        public virtual DbSet<Project> Articles { get; set; }
        public virtual DbSet<Town> Towns { get; set; }

        public HousingDbContext(DbContextOptions<HousingDbContext> options)
           : base(options)
        {
            
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();

            base.OnConfiguring(optionsBuilder);
        }
    }
}
