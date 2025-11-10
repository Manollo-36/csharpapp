namespace CSharpApp.Core.Interfaces;

public interface IApiClient
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
    Task<T?> PostAsync<T>(string endpoint, object request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
    Task<T?> PutAsync<T>(string endpoint, object request, CancellationToken cancellationToken = default);
}