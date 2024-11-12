using Microsoft.EntityFrameworkCore;

namespace QuoteApi.Data
{
    public class QuoteContext : DbContext
    {
        public QuoteContext(DbContextOptions<QuoteContext> options)
            : base(options)
        {

        }

        public DbSet<Quote> Quotes { get; set; }
    }
}
