namespace Frontend.DTOs;
public class QuoteDTO
{
    public int Id { get; set; }
    public string Quote { get; set; }
    public string SaidBy { get; set; }
    public string? When { get; set; }
    public UserInfoDTO? User { get; set; }
    public string? CreatedOn { get; set; }
}
