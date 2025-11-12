# Performance Monitoring Implementation

## Overview
The performance monitoring middleware has been successfully implemented in the C# application. This document describes the implementation and features.

## Components Implemented

### 1. Performance Settings Configuration
**File**: `CSharpApp.Core/Settings/PerformanceSettings.cs`
- Configurable slow request threshold (default: 1000ms)
- Option to log all requests or only slow ones
- Body size inclusion settings
- Maximum metrics history limit
- Detailed timing configuration

### 2. Performance Metrics Model
**File**: `CSharpApp.Core/Dtos/RequestPerformanceMetrics.cs`
- Captures comprehensive request data:
  - Request ID, timestamp, method, path
  - Status code, elapsed time
  - Request/response body sizes
  - User agent, IP address
  - Additional properties dictionary

### 3. Performance Metrics Service
**Files**: 
- `CSharpApp.Core/Interfaces/IPerformanceMetricsService.cs`
- `CSharpApp.Infrastructure/Services/PerformanceMetricsService.cs`

Features:
- Thread-safe metrics collection
- Real-time statistics calculation
- Slow request detection and alerting
- Memory-efficient with configurable history limit
- Comprehensive statistics including:
  - Total requests, slow requests, error requests
  - Average, median, min, max response times
  - Request distribution by endpoint
  - Response code distribution

### 4. Performance Logging Middleware
**File**: `CSharpApp.Infrastructure/Middleware/PerformanceLoggingMiddleware.cs`

Capabilities:
- Automatic request performance measurement
- Response body size capture
- Request ID generation and tracking
- Exception handling and logging
- Skips logging for health checks and static resources
- Integrates with IPerformanceMetricsService

### 5. Configuration Integration
**Updated Files**:
- `CSharpApp.Infrastructure/Configuration/DefaultConfiguration.cs` - Service registration
- `CSharpApp.Api/Program.cs` - Middleware pipeline registration
- `CSharpApp.Api/appsettings.json` - Performance configuration section

## API Endpoints Added

The following performance monitoring endpoints have been added:

### GET /api/v1.0/performance/metrics
Returns comprehensive performance statistics:
```json
{
  "totalRequests": 42,
  "slowRequests": 3,
  "errorRequests": 1,
  "averageResponseTimeMs": 245.5,
  "medianResponseTimeMs": 180.0,
  "maxResponseTimeMs": 1250,
  "minResponseTimeMs": 45,
  "firstRequestTime": "2025-11-12T14:30:00Z",
  "lastRequestTime": "2025-11-12T14:45:00Z",
  "requestsByEndpoint": {
    "GET /api/v1.0/products": 15,
    "GET /api/v1.0/categories": 8,
    "POST /api/v1.0/products": 3
  },
  "responseCodeDistribution": {
    "200": 35,
    "201": 3,
    "404": 2,
    "500": 1
  }
}
```

### GET /api/v1.0/performance/recent?count=50
Returns the most recent performance metrics (default: 50 requests)

### GET /api/v1.0/performance/slow?thresholdMs=1000
Returns requests that exceeded the specified threshold (default: 1000ms)

### DELETE /api/v1.0/performance/metrics
Clears all collected performance metrics

## Configuration Example

In `appsettings.json`:
```json
{
  "Performance": {
    "SlowRequestThresholdMs": 1000,
    "LogAllRequests": true,
    "IncludeBodySizes": true,
    "IncludeHeaders": false,
    "MaxMetricsHistory": 1000,
    "EnableDetailedTiming": true
  }
}
```

## Logging Output

The middleware generates structured logs for each request:
```json
{
  "@t": "2025-11-12T14:30:15.123Z",
  "@m": "Request Performance: GET /api/v1.0/products | Status: 200 | Duration: 245ms | RequestSize: 0b | ResponseSize: 15432b | RequestId: abc12345",
  "Method": "GET",
  "Path": "/api/v1.0/products",
  "StatusCode": 200,
  "Duration": 245,
  "RequestSize": 0,
  "ResponseSize": 15432,
  "RequestId": "abc12345"
}
```

For slow requests (exceeding threshold):
```json
{
  "@t": "2025-11-12T14:30:20.456Z",
  "@m": "Slow request detected: HTTP GET /api/v1.0/products responded 200 in 1250ms (Request: 0 bytes, Response: 15432 bytes)",
  "@l": "Warning"
}
```

## Features

1. **Real-time Monitoring**: Captures performance data for every API request
2. **Slow Request Detection**: Automatically identifies and alerts on slow requests
3. **Comprehensive Metrics**: Tracks timing, sizes, status codes, and more
4. **Memory Efficient**: Configurable history limits prevent memory bloat
5. **Thread Safe**: Concurrent request handling with proper synchronization
6. **Structured Logging**: Integration with Serilog for structured log output
7. **Statistical Analysis**: Real-time calculation of averages, medians, and distributions
8. **Endpoint Analysis**: Per-endpoint performance tracking
9. **Error Tracking**: Separate tracking of failed requests
10. **Configurable**: Extensive configuration options for different environments

## Usage Examples

### Monitor API Performance
```bash
# Get overall performance statistics
curl http://localhost:5225/api/v1.0/performance/metrics

# Check recent request performance
curl http://localhost:5225/api/v1.0/performance/recent?count=20

# Find slow requests
curl http://localhost:5225/api/v1.0/performance/slow?thresholdMs=500

# Clear metrics (useful for testing)
curl -X DELETE http://localhost:5225/api/v1.0/performance/metrics
```

### Log Analysis
The middleware integrates with the existing Serilog configuration, so performance logs will appear alongside other application logs with consistent formatting and structure.

## Benefits

1. **Performance Visibility**: Complete visibility into API performance
2. **Problem Detection**: Automatic identification of performance issues
3. **Trend Analysis**: Historical data for performance trend analysis
4. **Debugging Support**: Request IDs for correlation across logs
5. **Monitoring Integration**: Structured data ready for monitoring systems
6. **Production Ready**: Thread-safe, memory-efficient, and configurable

## Next Steps

The performance monitoring middleware is now fully implemented and ready for use. It will automatically begin collecting metrics once the application is started. Consider:

1. Setting up alerting based on slow request logs
2. Integrating with monitoring dashboards
3. Adjusting thresholds based on application requirements
4. Using request IDs for distributed tracing correlation