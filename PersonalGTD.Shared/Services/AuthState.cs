namespace PersonalGTD.Shared.Services;

public class AuthState
{
    public string? User { get; set; }
    public string? Token { get; set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
}
