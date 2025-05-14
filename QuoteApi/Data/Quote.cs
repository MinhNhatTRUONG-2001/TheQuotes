using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuoteApi.Data
{
    public class Quote
    {
        [Key]
        public int id { get; set; }
        public string quote_content { get; set; }
        public string who_said { get; set; }
        public DateOnly? when_was_said { get; set; }
        public string? source { get; set; }
        public int user_id { get; set; }
        [ForeignKey("user_id")]
        public User User { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? last_updated { get; set; }
        public ICollection<FavouriteQuote> id_favourite_quotes_quote_id { get; set; }
    }
}
