using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CSharpApp.Infrastructure.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;
    private readonly IAuthenticationService _authenticationService;

    public ApiClient(IHttpClientFactory httpClientFactory, 
        ILogger<ApiClient> logger,
        IAuthenticationService authenticationService)
    {
        _httpClient = httpClientFactory.CreateClient("ExternalApi");
        _logger = logger;
        _authenticationService = authenticationService;
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            
            return await ProcessResponseAsync<T>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during GET request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            
            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during POST request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object request, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            
            return await ProcessResponseAsync<T>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during POST request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            
            var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during DELETE request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PutAsync<T>(string endpoint, object request, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            
            var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
            
            return await ProcessResponseAsync<T>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during PUT request to {Endpoint}", endpoint);
            throw;
        }
    }

    private async Task<T?> ProcessResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("HTTP request failed with status {StatusCode}: {Content}", response.StatusCode, content);
            
            var errorMessage = response.StatusCode switch
            {
                HttpStatusCode.NotFound => "Resource not found",
                HttpStatusCode.Unauthorized => "Unauthorized access",
                HttpStatusCode.Forbidden => "Access forbidden",
                HttpStatusCode.BadRequest => "Bad request",
                HttpStatusCode.InternalServerError => "Internal server error",
                _ => $"HTTP request failed with status {response.StatusCode}"
            };
            
            throw new HttpRequestException(errorMessage);
        }

        if (string.IsNullOrEmpty(content))
        {
            return default;
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            return JsonSerializer.Deserialize<T>(content, options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize response content: {Content}", content);
            throw new InvalidOperationException("Failed to parse response", ex);
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        var token = await _authenticationService.GetAccessTokenAsync(cancellationToken);
        
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
}