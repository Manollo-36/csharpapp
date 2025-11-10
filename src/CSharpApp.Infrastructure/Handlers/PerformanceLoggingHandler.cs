using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CSharpApp.Infrastructure.Handlers;

public class PerformanceLoggingHandler : DelegatingHandler
{
    private readonly ILogger<PerformanceLoggingHandler> _logger;

    public PerformanceLoggingHandler(ILogger<PerformanceLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString()[..8];

        _logger.LogInformation(
            "HTTP Request [{RequestId}] {Method} {Uri} started",
            requestId,
            request.Method,
            request.RequestUri);

        HttpResponseMessage? response = null;
        Exception? exception = null;

        try
        {
            response = await base.SendAsync(request, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var logLevel = DetermineLogLevel(response, exception, stopwatch.ElapsedMilliseconds);

            _logger.Log(logLevel,
                "HTTP Request [{RequestId}] {Method} {Uri} completed in {ElapsedMilliseconds}ms " +
                "with status {StatusCode} {ReasonPhrase}",
                requestId,
                request.Method,
                request.RequestUri,
                stopwatch.ElapsedMilliseconds,
                response?.StatusCode.ToString() ?? "Error",
                response?.ReasonPhrase ?? exception?.GetType().Name);

            // Log performance warnings for slow requests
            if (stopwatch.ElapsedMilliseconds > 5000) // 5 seconds
            {
                _logger.LogWarning(
                    "Slow HTTP Request [{RequestId}] {Method} {Uri} took {ElapsedMilliseconds}ms",
                    requestId,
                    request.Method,
                    request.RequestUri,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private static LogLevel DetermineLogLevel(HttpResponseMessage? response, Exception? exception, long elapsedMilliseconds)
    {
        if (exception != null)
            return LogLevel.Error;

        if (response == null)
            return LogLevel.Warning;

        if (!response.IsSuccessStatusCode)
            return LogLevel.Warning;

        if (elapsedMilliseconds > 3000) // 3 seconds
            return LogLevel.Warning;

        return LogLevel.Information;
    }
}