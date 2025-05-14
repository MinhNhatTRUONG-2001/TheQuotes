using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuoteApi.Data
{
    public class FavouriteQuote
    {
        [Key]
        public Guid id { get; set; }
        public int user_id { get; set; }
        [ForeignKey("user_id")]
        public User User { get; set; }
        public int quote_id { get; set; }
        [ForeignKey("quote_id")]
        public Quote Quote { get; set; }
        public DateTime saved_at { get; set; }
    }
}