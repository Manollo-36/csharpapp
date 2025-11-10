namespace CSharpApp.Core.Interfaces;

public interface IAuthenticationService
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<bool> IsTokenValidAsync(CancellationToken cancellationToken = default);
    Task RefreshTokenAsync(CancellationToken cancellationToken = default);
}