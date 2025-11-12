using System.Collections.Concurrent;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Interfaces;
using CSharpApp.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace CSharpApp.Infrastructure.Services;

public class PerformanceMetricsService : IPerformanceMetricsService
{
    private readonly ConcurrentQueue<RequestPerformanceMetrics> _metrics = new();
    private readonly PerformanceSettings _settings;
    private readonly ILogger<PerformanceMetricsService> _logger;
    private readonly object _lockObject = new();

    public PerformanceMetricsService(
        IOptions<PerformanceSettings> settings,
        ILogger<PerformanceMetricsService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public void RecordRequest(RequestPerformanceMetrics metrics)
    {
        try
        {
            _metrics.Enqueue(metrics);

            // Maintain size limit
            while (_metrics.Count > _settings.MaxMetricsHistory)
            {
                _metrics.TryDequeue(out _);
            }

            // Log based on settings
            if (_settings.LogAllRequests || metrics.IsSlowRequest(_settings.SlowRequestThresholdMs))
            {
                var logLevel = metrics.IsSlowRequest(_settings.SlowRequestThresholdMs) ? LogLevel.Warning : LogLevel.Information;
                
                _logger.Log(logLevel, 
                    "Request Performance: {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | RequestSize: {RequestSize}b | ResponseSize: {ResponseSize}b | RequestId: {RequestId}",
                    metrics.Method, 
                    metrics.Path, 
                    metrics.StatusCode, 
                    metrics.ElapsedMilliseconds, 
                    metrics.RequestBodySize, 
                    metrics.ResponseBodySize, 
                    metrics.RequestId);

                if (metrics.IsSlowRequest(_settings.SlowRequestThresholdMs))
                {
                    _logger.LogWarning("Slow request detected: {Message}", metrics.GetLogMessage());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record performance metrics");
        }
    }

    public IEnumerable<RequestPerformanceMetrics> GetRecentMetrics(int count = 100)
    {
        return _metrics.TakeLast(count).OrderByDescending(m => m.Timestamp);
    }

    public IEnumerable<RequestPerformanceMetrics> GetSlowRequests(int thresholdMs)
    {
        return _metrics.Where(m => m.IsSlowRequest(thresholdMs))
                      .OrderByDescending(m => m.ElapsedMilliseconds);
    }

    public PerformanceStatistics GetStatistics()
    {
        lock (_lockObject)
        {
            var allMetrics = _metrics.ToArray();
            
            if (!allMetrics.Any())
            {
                return new PerformanceStatistics();
            }

            var responseTimes = allMetrics.Select(m => m.ElapsedMilliseconds).ToArray();
            var sortedTimes = responseTimes.OrderBy(t => t).ToArray();

            return new PerformanceStatistics
            {
                TotalRequests = allMetrics.Length,
                SlowRequests = allMetrics.Count(m => m.IsSlowRequest(_settings.SlowRequestThresholdMs)),
                ErrorRequests = allMetrics.Count(m => m.StatusCode >= 400),
                AverageResponseTimeMs = responseTimes.Length > 0 ? responseTimes.Sum() / (double)responseTimes.Length : 0,
                MedianResponseTimeMs = GetMedian(sortedTimes),
                MaxResponseTimeMs = responseTimes.Length > 0 ? responseTimes.Max() : 0,
                MinResponseTimeMs = responseTimes.Length > 0 ? responseTimes.Min() : 0,
                FirstRequestTime = allMetrics.Min(m => m.Timestamp),
                LastRequestTime = allMetrics.Max(m => m.Timestamp),
                RequestsByEndpoint = allMetrics.GroupBy(m => $"{m.Method} {m.Path}")
                                               .ToDictionary(g => g.Key, g => g.Count()),
                ResponseCodeDistribution = allMetrics.GroupBy(m => m.StatusCode)
                                                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }

    public void ClearMetrics()
    {
        lock (_lockObject)
        {
            while (_metrics.TryDequeue(out _)) { }
            _logger.LogInformation("Performance metrics cleared");
        }
    }

    private static double GetMedian(long[] sortedValues)
    {
        if (!sortedValues.Any()) return 0;
        
        var mid = sortedValues.Length / 2;
        return sortedValues.Length % 2 == 0 
            ? (sortedValues[mid - 1] + sortedValues[mid]) / 2.0 
            : sortedValues[mid];
    }
}