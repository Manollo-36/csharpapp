using CSharpApp.Core.Dtos;

namespace CSharpApp.Core.Interfaces;

public interface IPerformanceMetricsService
{
    void RecordRequest(RequestPerformanceMetrics metrics);
    IEnumerable<RequestPerformanceMetrics> GetRecentMetrics(int count = 100);
    IEnumerable<RequestPerformanceMetrics> GetSlowRequests(int thresholdMs);
    PerformanceStatistics GetStatistics();
    void ClearMetrics();
}

public class PerformanceStatistics
{
    public int TotalRequests { get; set; }
    public int SlowRequests { get; set; }
    public int ErrorRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double MedianResponseTimeMs { get; set; }
    public long MaxResponseTimeMs { get; set; }
    public long MinResponseTimeMs { get; set; }
    public DateTime FirstRequestTime { get; set; }
    public DateTime LastRequestTime { get; set; }
    public Dictionary<string, int> RequestsByEndpoint { get; set; } = new();
    public Dictionary<int, int> ResponseCodeDistribution { get; set; } = new();
}