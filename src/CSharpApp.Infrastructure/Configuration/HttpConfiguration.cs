using Polly;
using Polly.Extensions.Http;
using CSharpApp.Infrastructure.Handlers;

namespace CSharpApp.Infrastructure.Configuration;

public static class HttpConfiguration
{
    public static IServiceCollection AddHttpConfiguration(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetService<IConfiguration>();
        var restApiSettings = configuration!.GetSection(nameof(RestApiSettings)).Get<RestApiSettings>();
        var httpClientSettings = configuration.GetSection(nameof(HttpClientSettings)).Get<HttpClientSettings>();

        // Register performance logging handler
        services.AddTransient<PerformanceLoggingHandler>();

        // Register HttpClient for the external API with named client
        services.AddHttpClient("ExternalApi", client =>
        {
            client.BaseAddress = new Uri(restApiSettings!.BaseUrl!);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "CSharpApp/1.0");
        })
        .AddHttpMessageHandler<PerformanceLoggingHandler>()
        .AddPolicyHandler(GetRetryPolicy(httpClientSettings!))
        .AddPolicyHandler(GetCircuitBreakerPolicy())
        .AddPolicyHandler(GetTimeoutPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(HttpClientSettings settings)
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: settings.RetryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(settings.SleepDuration * retryAttempt),
                onRetry: (outcome, duration, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {duration}ms delay due to: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
    }
}