using Microsoft.EntityFrameworkCore;

namespace QuoteApi.Data
{
    public class QuoteContext : DbContext
    {
        public QuoteContext(DbContextOptions<QuoteContext> options)
            : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Quote> Quotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Quote>().ToTable("quotes");
            base.OnModelCreating(modelBuilder);
        }
    }
}