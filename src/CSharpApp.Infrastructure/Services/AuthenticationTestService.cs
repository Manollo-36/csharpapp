using Microsoft.Extensions.Logging;

namespace CSharpApp.Infrastructure.Services;

public class AuthenticationTestService
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthenticationTestService> _logger;

    public AuthenticationTestService(IAuthenticationService authenticationService, ILogger<AuthenticationTestService> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<AuthTestResult> TestAuthenticationAsync()
    {
        try
        {
            _logger.LogInformation("Testing JWT authentication...");
            
            var token = await _authenticationService.GetAccessTokenAsync();
            
            if (string.IsNullOrEmpty(token))
            {
                return new AuthTestResult(false, "Failed to obtain access token", null);
            }

            var isValid = await _authenticationService.IsTokenValidAsync();
            
            return new AuthTestResult(true, "Authentication successful", token.Length > 10 ? token[..10] + "..." : token);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Authentication failed: {Message}", ex.Message);
            return new AuthTestResult(false, $"Authentication failed: {ex.Message}", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication test failed");
            return new AuthTestResult(false, $"Authentication test error: {ex.Message}", null);
        }
    }
}

public record AuthTestResult(bool Success, string Message, string? TokenPreview);