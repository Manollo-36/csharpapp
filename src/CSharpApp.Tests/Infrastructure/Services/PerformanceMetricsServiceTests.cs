using CSharpApp.Infrastructure.Services;

namespace CSharpApp.Tests.Infrastructure.Services;

public class PerformanceMetricsServiceTests
{
    private readonly Mock<IOptions<PerformanceSettings>> _mockOptions;
    private readonly Mock<ILogger<PerformanceMetricsService>> _mockLogger;
    private readonly PerformanceMetricsService _service;
    private readonly PerformanceSettings _settings;

    public PerformanceMetricsServiceTests()
    {
        _mockOptions = new Mock<IOptions<PerformanceSettings>>();
        _mockLogger = new Mock<ILogger<PerformanceMetricsService>>();
        
        _settings = new PerformanceSettings
        {
            SlowRequestThresholdMs = 1000,
            LogAllRequests = true,
            MaxMetricsHistory = 100
        };
        
        _mockOptions.Setup(x => x.Value).Returns(_settings);
        
        _service = new PerformanceMetricsService(_mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public void RecordRequest_ShouldAddMetrics_WhenValidMetricsProvided()
    {
        // Arrange
        var metrics = new RequestPerformanceMetrics
        {
            RequestId = "test-id",
            Method = "GET",
            Path = "/api/test",
            StatusCode = 200,
            ElapsedMilliseconds = 500,
            Timestamp = DateTime.UtcNow
        };

        // Act
        _service.RecordRequest(metrics);
        var recentMetrics = _service.GetRecentMetrics(10);

        // Assert
        recentMetrics.Should().HaveCount(1);
        recentMetrics.First().Should().BeEquivalentTo(metrics);
    }

    [Fact]
    public void RecordRequest_ShouldLimitHistorySize_WhenMaxHistoryExceeded()
    {
        // Arrange
        var maxHistory = 5;
        _settings.MaxMetricsHistory = maxHistory;
        
        // Act - Add more metrics than the limit
        for (int i = 0; i < maxHistory + 3; i++)
        {
            var metrics = new RequestPerformanceMetrics
            {
                RequestId = $"test-id-{i}",
                Method = "GET",
                Path = $"/api/test/{i}",
                StatusCode = 200,
                ElapsedMilliseconds = 100 + i,
                Timestamp = DateTime.UtcNow.AddMilliseconds(i)
            };
            _service.RecordRequest(metrics);
        }

        var recentMetrics = _service.GetRecentMetrics(100);

        // Assert
        recentMetrics.Should().HaveCount(maxHistory);
    }

    [Fact]
    public void GetSlowRequests_ShouldReturnOnlySlowRequests_WhenThresholdProvided()
    {
        // Arrange
        var fastRequest = new RequestPerformanceMetrics
        {
            RequestId = "fast",
            Method = "GET",
            Path = "/api/fast",
            StatusCode = 200,
            ElapsedMilliseconds = 500,
            Timestamp = DateTime.UtcNow
        };

        var slowRequest = new RequestPerformanceMetrics
        {
            RequestId = "slow",
            Method = "GET",
            Path = "/api/slow",
            StatusCode = 200,
            ElapsedMilliseconds = 1500,
            Timestamp = DateTime.UtcNow
        };

        // Act
        _service.RecordRequest(fastRequest);
        _service.RecordRequest(slowRequest);
        
        var slowRequests = _service.GetSlowRequests(1000);

        // Assert
        slowRequests.Should().HaveCount(1);
        slowRequests.First().RequestId.Should().Be("slow");
    }

    [Fact]
    public void GetStatistics_ShouldCalculateCorrectStatistics_WhenMultipleRequests()
    {
        // Arrange
        var requests = new[]
        {
            new RequestPerformanceMetrics { ElapsedMilliseconds = 100, StatusCode = 200, Method = "GET", Path = "/api/test1", Timestamp = DateTime.UtcNow },
            new RequestPerformanceMetrics { ElapsedMilliseconds = 200, StatusCode = 200, Method = "GET", Path = "/api/test2", Timestamp = DateTime.UtcNow },
            new RequestPerformanceMetrics { ElapsedMilliseconds = 1500, StatusCode = 200, Method = "POST", Path = "/api/test1", Timestamp = DateTime.UtcNow },
            new RequestPerformanceMetrics { ElapsedMilliseconds = 300, StatusCode = 404, Method = "GET", Path = "/api/test3", Timestamp = DateTime.UtcNow }
        };

        // Act
        foreach (var request in requests)
        {
            _service.RecordRequest(request);
        }

        var statistics = _service.GetStatistics();

        // Assert
        statistics.TotalRequests.Should().Be(4);
        statistics.SlowRequests.Should().Be(1); // Only the 1500ms request
        statistics.ErrorRequests.Should().Be(1); // Only the 404 request
        statistics.AverageResponseTimeMs.Should().Be(525); // (100+200+1500+300)/4
        statistics.MaxResponseTimeMs.Should().Be(1500);
        statistics.MinResponseTimeMs.Should().Be(100);
        statistics.RequestsByEndpoint.Should().ContainKey("GET /api/test1");
        statistics.RequestsByEndpoint.Should().ContainKey("POST /api/test1");
        statistics.ResponseCodeDistribution.Should().ContainKey(200);
        statistics.ResponseCodeDistribution.Should().ContainKey(404);
        statistics.ResponseCodeDistribution[200].Should().Be(3);
        statistics.ResponseCodeDistribution[404].Should().Be(1);
    }

    [Fact]
    public void GetStatistics_ShouldReturnEmptyStatistics_WhenNoRequests()
    {
        // Act
        var statistics = _service.GetStatistics();

        // Assert
        statistics.TotalRequests.Should().Be(0);
        statistics.SlowRequests.Should().Be(0);
        statistics.ErrorRequests.Should().Be(0);
        statistics.RequestsByEndpoint.Should().BeEmpty();
        statistics.ResponseCodeDistribution.Should().BeEmpty();
    }

    [Fact]
    public void ClearMetrics_ShouldRemoveAllMetrics()
    {
        // Arrange
        var metrics = new RequestPerformanceMetrics
        {
            RequestId = "test",
            Method = "GET",
            Path = "/api/test",
            StatusCode = 200,
            ElapsedMilliseconds = 500,
            Timestamp = DateTime.UtcNow
        };
        
        _service.RecordRequest(metrics);

        // Act
        _service.ClearMetrics();
        var recentMetrics = _service.GetRecentMetrics(10);
        var statistics = _service.GetStatistics();

        // Assert
        recentMetrics.Should().BeEmpty();
        statistics.TotalRequests.Should().Be(0);
    }

    [Theory]
    [InlineData(500, false)]
    [InlineData(1000, false)]
    [InlineData(1001, true)]
    [InlineData(2000, true)]
    public void IsSlowRequest_ShouldReturnCorrectResult_ForDifferentThresholds(long elapsedMs, bool expectedSlow)
    {
        // Arrange
        var metrics = new RequestPerformanceMetrics
        {
            ElapsedMilliseconds = elapsedMs
        };

        // Act
        var isSlowRequest = metrics.IsSlowRequest(_settings.SlowRequestThresholdMs);

        // Assert
        isSlowRequest.Should().Be(expectedSlow);
    }

    [Fact]
    public void GetLogMessage_ShouldFormatCorrectly()
    {
        // Arrange
        var metrics = new RequestPerformanceMetrics
        {
            Method = "POST",
            Path = "/api/products",
            StatusCode = 201,
            ElapsedMilliseconds = 750,
            RequestBodySize = 1024,
            ResponseBodySize = 512
        };

        // Act
        var logMessage = metrics.GetLogMessage();

        // Assert
        logMessage.Should().Be("HTTP POST /api/products responded 201 in 750ms (Request: 1024 bytes, Response: 512 bytes)");
    }
}