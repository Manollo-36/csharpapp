namespace CSharpApp.Core.Dtos;

public sealed class AuthenticationRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthenticationResponse
{
    [JsonPropertyName("access_token")]
    public string Access_token { get; set; } = string.Empty;
    
    [JsonPropertyName("refresh_token")]
    public string Refresh_token { get; set; } = string.Empty;
}