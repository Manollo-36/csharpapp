using CSharpApp.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace CSharpApp.Tests.Infrastructure.Middleware;

public class PerformanceLoggingMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<PerformanceLoggingMiddleware>> _mockLogger;
    private readonly Mock<IOptions<PerformanceSettings>> _mockOptions;
    private readonly Mock<IPerformanceMetricsService> _mockMetricsService;
    private readonly PerformanceSettings _settings;
    private readonly PerformanceLoggingMiddleware _middleware;

    public PerformanceLoggingMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<PerformanceLoggingMiddleware>>();
        _mockOptions = new Mock<IOptions<PerformanceSettings>>();
        _mockMetricsService = new Mock<IPerformanceMetricsService>();
        
        _settings = new PerformanceSettings
        {
            SlowRequestThresholdMs = 1000,
            LogAllRequests = true,
            IncludeBodySizes = true,
            IncludeHeaders = false
        };
        
        _mockOptions.Setup(x => x.Value).Returns(_settings);
        
        _middleware = new PerformanceLoggingMiddleware(_mockNext.Object, _mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenValidRequest()
    {
        // Arrange
        var context = CreateHttpContext();
        
        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockMetricsService.Object);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRecordMetrics_WhenRequestCompletes()
    {
        // Arrange
        var context = CreateHttpContext();
        
        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockMetricsService.Object);

        // Assert
        _mockMetricsService.Verify(x => x.RecordRequest(It.IsAny<RequestPerformanceMetrics>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetRequestId_InResponseHeaders()
    {
        // Arrange
        var context = CreateHttpContext();
        
        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockMetricsService.Object);

        // Assert
        context.Response.Headers.Should().ContainKey("X-Request-Id");
        context.Response.Headers["X-Request-Id"].Should().NotBeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_ShouldRecordMetricsWithCorrectPath_WhenRequestHasPath()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/v1.0/products";
        context.Request.Method = "GET";
        
        RequestPerformanceMetrics capturedMetrics = null!;
        _mockMetricsService
            .Setup(x => x.RecordRequest(It.IsAny<RequestPerformanceMetrics>()))
            .Callback<RequestPerformanceMetrics>(metrics => capturedMetrics = metrics);

        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockMetricsService.Object);

        // Assert
        capturedMetrics.Should().NotBeNull();
        capturedMetrics.Path.Should().Be("/api/v1.0/products");
        capturedMetrics.Method.Should().Be("GET");
        capturedMetrics.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ShouldSkipLogging_ForHealthCheckEndpoints()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/health";

        // Act
        await _middleware.InvokeAsync(context, _mockMetricsService.Object);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        _mockMetricsService.Verify(x => x.RecordRequest(It.IsAny<RequestPerformanceMetrics>()), Times.Never);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/metrics")]
    [InlineData("/swagger")]
    [InlineData("/favicon.ico")]
    [InlineData("/_vs/browserLink")]
    [InlineData("/_framework/blazor")]
    public async Task InvokeAsync_ShouldSkipLogging_ForSpecificPaths(string path)
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = path;

        // Act
        await _middleware.InvokeAsync(context, _mockMetricsService.Object);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        _mockMetricsService.Verify(x => x.RecordRequest(It.IsAny<RequestPerformanceMetrics>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRecordMetrics_EvenWhenExceptionOccurs()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Test exception");
        
        RequestPerformanceMetrics capturedMetrics = null!;
        _mockMetricsService
            .Setup(x => x.RecordRequest(It.IsAny<RequestPerformanceMetrics>()))
            .Callback<RequestPerformanceMetrics>(metrics => capturedMetrics = metrics);

        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act & Assert
        await _middleware.Invoking(x => x.InvokeAsync(context, _mockMetricsService.Object))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");

        // Assert metrics were still recorded
        _mockMetricsService.Verify(x => x.RecordRequest(It.IsAny<RequestPerformanceMetrics>()), Times.Once);
        capturedMetrics.Should().NotBeNull();
        capturedMetrics.StatusCode.Should().Be(500);
        capturedMetrics.AdditionalProperties.Should().ContainKey("Exception");
        capturedMetrics.AdditionalProperties["Exception"].Should().Be("InvalidOperationException");
    }

    [Fact]
    public async Task InvokeAsync_ShouldMeasureElapsedTime_Accurately()
    {
        // Arrange
        var context = CreateHttpContext();
        var delay = TimeSpan.FromMilliseconds(100);
        
        RequestPerformanceMetrics capturedMetrics = null!;
        _mockMetricsService
            .Setup(x => x.RecordRequest(It.IsAny<RequestPerformanceMetrics>()))
            .Callback<RequestPerformanceMetrics>(metrics => capturedMetrics = metrics);

        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(async () => await Task.Delay(delay));

        // Act
        await _middleware.InvokeAsync(context, _mockMetricsService.Object);

        // Assert
        capturedMetrics.Should().NotBeNull();
        capturedMetrics.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(delay.Milliseconds - 50); // Allow some tolerance
        capturedMetrics.ElapsedMilliseconds.Should().BeLessThan(delay.Milliseconds + 100);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCaptureUserAgent_WhenPresent()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Headers["User-Agent"] = "Test-Agent/1.0";
        
        RequestPerformanceMetrics capturedMetrics = null!;
        _mockMetricsService
            .Setup(x => x.RecordRequest(It.IsAny<RequestPerformanceMetrics>()))
            .Callback<RequestPerformanceMetrics>(metrics => capturedMetrics = metrics);

        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockMetricsService.Object);

        // Assert
        capturedMetrics.Should().NotBeNull();
        capturedMetrics.UserAgent.Should().Be("Test-Agent/1.0");
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost:5000");
        context.Response.StatusCode = 200;
        context.Response.Body = new MemoryStream();
        
        return context;
    }
}