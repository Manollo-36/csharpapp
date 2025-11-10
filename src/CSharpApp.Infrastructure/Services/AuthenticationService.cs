using System.Net.Mime;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSharpApp.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<AuthenticationService> _logger;
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public AuthenticationService(IHttpClientFactory httpClientFactory,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ExternalApi");
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_accessToken) || !await IsTokenValidAsync(cancellationToken))
        {
            await AuthenticateAsync(cancellationToken);
        }

        return _accessToken;
    }

    public Task<bool> IsTokenValidAsync(CancellationToken cancellationToken = default)
    {
        // Add a 5-minute buffer before expiry
        var isValid = !string.IsNullOrEmpty(_accessToken) && 
                      DateTime.UtcNow < _tokenExpiry.AddMinutes(-5);
        
        return Task.FromResult(isValid);
    }

    public async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await AuthenticateAsync(cancellationToken);
    }

    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var authRequest = new AuthenticationRequest
            {
                Email = _restApiSettings.Username!,
                Password = _restApiSettings.Password!
            };

            var json = JsonSerializer.Serialize(authRequest);
            var content = new StringContent(json);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            _logger.LogInformation("Attempting to authenticate with external API");

            var response = await _httpClient.PostAsync(_restApiSettings.Auth, content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (authResponse != null)
                {
                    _accessToken = authResponse.Access_token;
                    _refreshToken = authResponse.Refresh_token;
                    
                    // Set token expiry (assuming 1 hour validity, adjust as needed)
                    _tokenExpiry = DateTime.UtcNow.AddHours(1);
                    
                    _logger.LogInformation("Successfully authenticated with external API");
                }
                else
                {
                    _logger.LogError("Failed to parse authentication response");
                    throw new InvalidOperationException("Authentication failed - invalid response format");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Authentication failed with status {StatusCode}: {Content}", 
                    response.StatusCode, errorContent);
                throw new UnauthorizedAccessException("Authentication failed");
            }
        }
        catch (Exception ex) when (!(ex is UnauthorizedAccessException))
        {
            _logger.LogError(ex, "Error occurred during authentication");
            throw new InvalidOperationException("Authentication process failed", ex);
        }
    }
}