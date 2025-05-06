public class TokenService
{
    private string? _token;
    public string? Token => _token;

    public event Action? OnTokenChanged;

    public void SetToken(string? token)
    {
        _token = token;
        OnTokenChanged?.Invoke();
    }
}