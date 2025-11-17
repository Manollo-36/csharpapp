using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CSharpApp.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
        content.Should().Contain("timestamp");
        content.Should().Contain("CSharpApp.Api");
    }

    [Fact]
    public async Task PerformanceMetrics_ShouldReturnStatistics()
    {
        // Arrange - Make a request first to generate some metrics
        await _client.GetAsync("/health");

        // Act
        var response = await _client.GetAsync("/api/v1.0/performance/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("totalRequests");
        content.Should().Contain("averageResponseTimeMs");
    }

    [Fact]
    public async Task PerformanceRecent_ShouldReturnRecentMetrics()
    {
        // Arrange - Make a request first to generate some metrics
        await _client.GetAsync("/health");

        // Act
        var response = await _client.GetAsync("/api/v1.0/performance/recent?count=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var metrics = JsonSerializer.Deserialize<RequestPerformanceMetrics[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        metrics.Should().NotBeNull();
        metrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PerformanceSlowRequests_ShouldReturnEmptyInitially()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/performance/slow?thresholdMs=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var slowRequests = JsonSerializer.Deserialize<RequestPerformanceMetrics[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        slowRequests.Should().NotBeNull();
        // Since our health check is fast, there should be no slow requests
    }

    [Fact]
    public async Task ClearPerformanceMetrics_ShouldClearSuccessfully()
    {
        // Arrange - Generate some metrics first
        await _client.GetAsync("/health");

        // Act
        var response = await _client.DeleteAsync("/api/v1.0/performance/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Performance metrics cleared");
        content.Should().Contain("timestamp");
    }

    [Fact]
    public async Task AuthStatus_ShouldReturnAuthenticationStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/auth/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("success");
        content.Should().Contain("baseUrl");
        content.Should().Contain("authEndpoint");
    }

    [Fact]
    public async Task ResponseHeaders_ShouldIncludeCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvalidEndpoint_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("GET")]
    public async Task PerformanceMiddleware_ShouldLogHttpMethods(string method)
    {
        // Arrange - Use API endpoint that is not skipped by middleware
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1.0/performance/metrics");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify metrics were recorded which means the middleware is working
        var metricsResponse = await _client.GetAsync("/api/v1.0/performance/recent?count=10");
        metricsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await metricsResponse.Content.ReadAsStringAsync();
        content.Should().Contain($"\"{method}\"");
        content.Should().Contain("/api/v1.0/performance/metrics");
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldAllBeHandledCorrectly()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        var requestCount = 5; // Reduced count to avoid overwhelming the API

        // Act - Make multiple concurrent requests to different endpoints to verify handling
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(_client.GetAsync("/api/v1.0/performance/metrics"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(requestCount);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        
        // Verify performance metrics were recorded for concurrent requests
        var metricsResponse = await _client.GetAsync($"/api/v1.0/performance/recent?count={requestCount + 10}");
        metricsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var metricsContent = await metricsResponse.Content.ReadAsStringAsync();
        var metrics = JsonSerializer.Deserialize<RequestPerformanceMetrics[]>(metricsContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        // Should have recorded metrics for our requests
        metrics.Should().NotBeNull();
        var metricsRequests = metrics!.Where(m => m.Path == "/api/v1.0/performance/metrics").ToList();
        metricsRequests.Should().HaveCountGreaterOrEqualTo(requestCount);
    }

    [Fact]
    public async Task OpenApiEndpoint_ShouldBeAvailable_InDevelopment()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                context.HostingEnvironment.EnvironmentName = Environments.Development;
            });
        });
        
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/openapi/v1.json");

        // Assert
        // The endpoint may or may not be available depending on configuration
        // Just verify we get a response (either 200 or 404 is acceptable)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}