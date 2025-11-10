namespace CSharpApp.Core.Dtos;

public sealed class AuthenticationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthenticationResponse
{
    public string Access_token { get; set; } = string.Empty;
    public string Refresh_token { get; set; } = string.Empty;
}