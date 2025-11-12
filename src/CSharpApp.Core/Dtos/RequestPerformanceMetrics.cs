namespace CSharpApp.Core.Dtos;

public class RequestPerformanceMetrics
{
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public int StatusCode { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public long RequestBodySize { get; set; }
    public long ResponseBodySize { get; set; }
    public string? UserAgent { get; set; }
    public string? RemoteIpAddress { get; set; }
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
    
    public bool IsSlowRequest(int thresholdMs) => ElapsedMilliseconds > thresholdMs;
    
    public string GetLogMessage()
    {
        return $"HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms " +
               $"(Request: {RequestBodySize} bytes, Response: {ResponseBodySize} bytes)";
    }
}