using System.ComponentModel.DataAnnotations;

namespace QuoteApi.Data
{
    public class User
    {
        [Key]
        public int id { get; set; }
        public string username { get; set; }
        public string displayed_name { get; set; }
        public string password { get; set; }
        public ICollection<Quote> quotes { get; set; }
    }
}
