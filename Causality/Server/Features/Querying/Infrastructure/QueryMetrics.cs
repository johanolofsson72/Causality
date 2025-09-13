using System.Diagnostics.Metrics;

namespace Causality.Server.Features.Querying.Infrastructure;

/// <summary>
/// Prometheus-compatible metrics for query execution monitoring
/// Implements ADR-0005 metrics requirements
/// </summary>
public class QueryMetrics
{
    private readonly Meter _meter;
    private readonly Histogram<double> _queryLatency;
    private readonly Counter<long> _queryExecutedTotal;
    private readonly Counter<long> _queryBlockedTotal;
    private readonly Histogram<long> _queryResultCount;
    private readonly UpDownCounter<long> _activeConcurrentQueries;

    public QueryMetrics()
    {
        _meter = new Meter("Causality.Query", "1.0.0");
        
        _queryLatency = _meter.CreateHistogram<double>(
            "query_latency_ms",
            "milliseconds", 
            "Histogram of query execution times in milliseconds");
            
        _queryExecutedTotal = _meter.CreateCounter<long>(
            "query_executed_total",
            description: "Total number of queries executed");
            
        _queryBlockedTotal = _meter.CreateCounter<long>(
            "query_blocked_total", 
            description: "Total number of queries blocked by validation");
            
        _queryResultCount = _meter.CreateHistogram<long>(
            "query_result_count",
            description: "Histogram of result counts per query");
            
        _activeConcurrentQueries = _meter.CreateUpDownCounter<long>(
            "active_concurrent_queries",
            description: "Number of currently executing queries");
    }

    /// <summary>
    /// Record query execution metrics
    /// </summary>
    public void RecordQueryExecution(
        string entity, 
        double latencyMs, 
        long resultCount, 
        bool fromCache = false,
        string? userId = null,
        string? tenantId = null)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("entity", entity),
            new("from_cache", fromCache),
            new("user_id", userId ?? "anonymous"),
            new("tenant_id", tenantId ?? "unknown")
        };

        _queryLatency.Record(latencyMs, tags);
        _queryExecutedTotal.Add(1, tags);
        _queryResultCount.Record(resultCount, tags);
    }

    /// <summary>
    /// Record blocked query
    /// </summary>
    public void RecordBlockedQuery(
        string entity, 
        string blockReason,
        string? userId = null,
        string? tenantId = null)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("entity", entity),
            new("block_reason", blockReason),
            new("user_id", userId ?? "anonymous"),
            new("tenant_id", tenantId ?? "unknown")
        };

        _queryBlockedTotal.Add(1, tags);
    }

    /// <summary>
    /// Track concurrent query execution
    /// </summary>
    public IDisposable TrackConcurrentQuery(string entity)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("entity", entity)
        };

        _activeConcurrentQueries.Add(1, tags);

        return new ConcurrentQueryTracker(() => _activeConcurrentQueries.Add(-1, tags));
    }

    private class ConcurrentQueryTracker : IDisposable
    {
        private readonly Action _onDispose;
        private bool _disposed;

        public ConcurrentQueryTracker(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _onDispose();
                _disposed = true;
            }
        }
    }
}

/// <summary>
/// Audit logging for query execution
/// Records all query activity for compliance and debugging
/// </summary>
public class QueryAuditLogger
{
    private readonly ILogger<QueryAuditLogger> _logger;

    public QueryAuditLogger(ILogger<QueryAuditLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Log query execution attempt
    /// </summary>
    public void LogQueryExecution(
        string queryHash,
        string entity,
        string userId,
        string tenantId,
        double elapsedMs,
        long rowCount,
        bool fromCache,
        bool successful = true,
        string? errorMessage = null)
    {
        var logData = new
        {
            QueryHash = queryHash,
            Entity = entity,
            UserId = userId,
            TenantId = tenantId,
            ElapsedMs = elapsedMs,
            RowCount = rowCount,
            FromCache = fromCache,
            Successful = successful,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };

        if (successful)
        {
            _logger.LogInformation("Query executed successfully: {LogData}", logData);
        }
        else
        {
            _logger.LogError("Query execution failed: {LogData}", logData);
        }
    }

    /// <summary>
    /// Log blocked query attempt
    /// </summary>
    public void LogBlockedQuery(
        string queryHash,
        string entity,
        string userId,
        string tenantId,
        string blockReason,
        List<string> validationErrors)
    {
        var logData = new
        {
            QueryHash = queryHash,
            Entity = entity,
            UserId = userId,
            TenantId = tenantId,
            BlockReason = blockReason,
            ValidationErrors = validationErrors,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogWarning("Query blocked due to validation: {LogData}", logData);
    }

    /// <summary>
    /// Log security event (e.g., attempted access to forbidden field)
    /// </summary>
    public void LogSecurityEvent(
        string eventType,
        string entity,
        string userId,
        string tenantId,
        string details)
    {
        var logData = new
        {
            EventType = eventType,
            Entity = entity,
            UserId = userId,
            TenantId = tenantId,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogWarning("Security event: {LogData}", logData);
    }
}