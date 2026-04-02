using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BillingSuite.Infrastructure.Logging;

/// <summary>
/// Extension methods for logging
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Logs the start of an operation with context
    /// </summary>
    public static IDisposable LogOperationStart(this ILogger logger, string operationName, object? context = null)
    {
        logger.LogInformation("Starting operation: {OperationName}", operationName);
        if (context != null)
        {
            logger.LogDebug("Operation context: {@Context}", context);
        }
        return new OperationScope(logger, operationName);
    }

    /// <summary>
    /// Logs business event (e.g., invoice created)
    /// </summary>
    public static void LogBusinessEvent(this ILogger logger, string eventName, object eventData)
    {
        logger.LogInformation("Business Event: {EventName} | {@EventData}", eventName, eventData);
    }

    /// <summary>
    /// Logs a data operation (CREATE, READ, UPDATE, DELETE)
    /// </summary>
    public static void LogDataOperation(this ILogger logger, string operation, string entityType, int? id = null, object? data = null)
    {
        var logMessage = id.HasValue 
            ? $"{operation} {entityType} (ID: {id})"
            : $"{operation} {entityType}";
        
        if (data != null)
        {
            logger.LogInformation("Data Operation: {Operation} | {@Data}", logMessage, data);
        }
        else
        {
            logger.LogInformation("Data Operation: {Operation}", logMessage);
        }
    }

    /// <summary>
    /// Logs performance metric
    /// </summary>
    public static void LogPerformance(this ILogger logger, string operationName, long elapsedMilliseconds, object? context = null)
    {
        var level = elapsedMilliseconds > 1000 ? LogLevel.Warning : LogLevel.Debug;
        logger.Log(level, "Performance: {OperationName} completed in {ElapsedMs}ms", operationName, elapsedMilliseconds);
        
        if (context != null)
        {
            logger.LogDebug("Performance context: {@Context}", context);
        }
    }

    /// <summary>
    /// Logs a user action
    /// </summary>
    public static void LogUserAction(this ILogger logger, string action, string? userId = null, object? data = null)
    {
        var message = userId != null ? $"User {userId} performed: {action}" : $"User performed: {action}";
        
        if (data != null)
        {
            logger.LogInformation("User Action: {Message} | {@Data}", message, data);
        }
        else
        {
            logger.LogInformation("User Action: {Message}", message);
        }
    }

    /// <summary>
    /// Logs validation error
    /// </summary>
    public static void LogValidationError(this ILogger logger, string entityType, string errorMessage, object? invalidData = null)
    {
        if (invalidData != null)
        {
            logger.LogWarning("Validation Error for {EntityType}: {ErrorMessage} | {@InvalidData}", entityType, errorMessage, invalidData);
        }
        else
        {
            logger.LogWarning("Validation Error for {EntityType}: {ErrorMessage}", entityType, errorMessage);
        }
    }

    /// <summary>
    /// Logs a critical operation error
    /// </summary>
    public static void LogOperationError(this ILogger logger, string operationName, Exception ex, object? context = null)
    {
        if (context != null)
        {
            logger.LogError(ex, "Operation '{OperationName}' failed with error: {ErrorMessage} | {@Context}", 
                operationName, ex.Message, context);
        }
        else
        {
            logger.LogError(ex, "Operation '{OperationName}' failed with error: {ErrorMessage}", 
                operationName, ex.Message);
        }
    }

    /// <summary>
    /// Logs database operation
    /// </summary>
    public static void LogDatabaseOperation(this ILogger logger, string operation, string tableName, int? recordCount = null, long? durationMs = null)
    {
        var details = new Dictionary<string, object?>
        {
            { "Operation", operation },
            { "Table", tableName },
            { "RecordCount", recordCount },
            { "DurationMs", durationMs }
        };
        
        logger.LogDebug("Database Operation: {Details}", string.Join(" | ", details.Where(x => x.Value != null).Select(x => $"{x.Key}={x.Value}")));
    }

    /// <summary>
    /// Scope for operation timing and logging
    /// </summary>
    private class OperationScope : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        public OperationScope(ILogger logger, string operationName)
        {
            _logger = logger;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _logger.LogPerformance(_operationName, _stopwatch.ElapsedMilliseconds);
        }
    }
}
