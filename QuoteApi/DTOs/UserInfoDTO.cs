namespace QuoteApi.DTOs
{
    public class UserInfoDTO
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? DisplayedName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
