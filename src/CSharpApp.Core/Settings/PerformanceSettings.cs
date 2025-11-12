namespace CSharpApp.Core.Settings;

public class PerformanceSettings
{
    public const string SectionName = "Performance";
    
    /// <summary>
    /// Threshold in milliseconds for slow request detection
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 1000;
    
    /// <summary>
    /// Whether to log all requests or only slow ones
    /// </summary>
    public bool LogAllRequests { get; set; } = true;
    
    /// <summary>
    /// Whether to include request/response body sizes in logs
    /// </summary>
    public bool IncludeBodySizes { get; set; } = true;
    
    /// <summary>
    /// Whether to include response headers in performance logs
    /// </summary>
    public bool IncludeHeaders { get; set; } = false;
    
    /// <summary>
    /// Maximum number of performance metrics to keep in memory
    /// </summary>
    public int MaxMetricsHistory { get; set; } = 1000;
    
    /// <summary>
    /// Whether to enable detailed timing breakdown
    /// </summary>
    public bool EnableDetailedTiming { get; set; } = true;
}