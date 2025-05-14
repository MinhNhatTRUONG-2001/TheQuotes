namespace QuoteApi.DTOs
{
    public class FavouriteQuoteDTO
    {
        public string? Id { get; set; }
        public int? UserId { get; set; }
        public int QuoteId { get; set; }
        public string? SavedAt { get; set; }
    }
}