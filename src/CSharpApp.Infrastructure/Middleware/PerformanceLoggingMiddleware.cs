using System.Diagnostics;
using System.Text;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Interfaces;
using CSharpApp.Core.Settings;
using Microsoft.Extensions.Options;

namespace CSharpApp.Infrastructure.Middleware;

public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;
    private readonly PerformanceSettings _settings;

    public PerformanceLoggingMiddleware(
        RequestDelegate next,
        ILogger<PerformanceLoggingMiddleware> logger,
        IOptions<PerformanceSettings> settings)
    {
        _next = next;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task InvokeAsync(HttpContext context, IPerformanceMetricsService metricsService)
    {
        // Skip performance logging for certain paths
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var requestId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();
        var originalResponseBodyStream = context.Response.Body;
        
        // Capture request details
        var requestBodySize = GetRequestBodySize(context.Request);
        
        try
        {
            // Create a new memory stream for the response body to capture its size
            using var responseBodyMemoryStream = new MemoryStream();
            context.Response.Body = responseBodyMemoryStream;

            // Add request ID to response headers
            context.Response.Headers.TryAdd("X-Request-Id", requestId);

            // Continue to the next middleware
            await _next(context);

            stopwatch.Stop();

            // Get response body size
            var responseBodySize = responseBodyMemoryStream.Length;

            // Copy response body back to original stream
            responseBodyMemoryStream.Seek(0, SeekOrigin.Begin);
            await responseBodyMemoryStream.CopyToAsync(originalResponseBodyStream);

            // Record performance metrics
            var metrics = CreatePerformanceMetrics(context, requestId, stopwatch.ElapsedMilliseconds, 
                                                 requestBodySize, responseBodySize);
            
            metricsService.RecordRequest(metrics);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Record metrics even for failed requests
            var metrics = CreatePerformanceMetrics(context, requestId, stopwatch.ElapsedMilliseconds, 
                                                 requestBodySize, 0);
            metrics.StatusCode = 500; // Override status code for exceptions
            metrics.AdditionalProperties["Exception"] = ex.GetType().Name;
            metrics.AdditionalProperties["ExceptionMessage"] = ex.Message;
            
            metricsService.RecordRequest(metrics);
            
            _logger.LogError(ex, "Request failed: {Method} {Path} in {Duration}ms (RequestId: {RequestId})",
                context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds, requestId);
            
            throw;
        }
        finally
        {
            context.Response.Body = originalResponseBodyStream;
        }
    }

    private RequestPerformanceMetrics CreatePerformanceMetrics(HttpContext context, string requestId, 
                                                             long elapsedMs, long requestBodySize, long responseBodySize)
    {
        return new RequestPerformanceMetrics
        {
            RequestId = requestId,
            Timestamp = DateTime.UtcNow,
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
            StatusCode = context.Response.StatusCode,
            ElapsedMilliseconds = elapsedMs,
            RequestBodySize = requestBodySize,
            ResponseBodySize = responseBodySize,
            UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
            RemoteIpAddress = GetRemoteIpAddress(context),
            AdditionalProperties = _settings.IncludeHeaders ? GetResponseHeaders(context) : new Dictionary<string, object>()
        };
    }

    private long GetRequestBodySize(HttpRequest request)
    {
        if (!_settings.IncludeBodySizes)
            return 0;

        try
        {
            if (request.ContentLength.HasValue)
            {
                return request.ContentLength.Value;
            }

            // For chunked requests without content-length
            if (request.Body.CanSeek)
            {
                var position = request.Body.Position;
                request.Body.Seek(0, SeekOrigin.End);
                var length = request.Body.Position;
                request.Body.Seek(position, SeekOrigin.Begin);
                return length;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private string? GetRemoteIpAddress(HttpContext context)
    {
        return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
            ?? context.Request.Headers["X-Real-IP"].FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString();
    }

    private Dictionary<string, object> GetResponseHeaders(HttpContext context)
    {
        var headers = new Dictionary<string, object>();
        
        foreach (var header in context.Response.Headers)
        {
            headers[header.Key] = header.Value.Count == 1 
                ? header.Value[0] ?? string.Empty
                : header.Value.ToArray();
        }
        
        return headers;
    }

    private static bool ShouldSkipLogging(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant();
        
        return pathValue != null && (
            pathValue.StartsWith("/health") ||
            pathValue.StartsWith("/metrics") ||
            pathValue.StartsWith("/swagger") ||
            pathValue.StartsWith("/favicon.ico") ||
            pathValue.StartsWith("/_vs/") ||
            pathValue.StartsWith("/_framework/")
        );
    }
}