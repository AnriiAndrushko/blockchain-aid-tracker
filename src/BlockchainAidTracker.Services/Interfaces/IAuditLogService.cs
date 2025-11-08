using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.AuditLog;

namespace BlockchainAidTracker.Services.Interfaces;

/// <summary>
/// Interface for audit logging service
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Logs a successful operation
    /// </summary>
    Task LogAsync(
        AuditLogCategory category,
        AuditLogAction action,
        string description,
        string? userId = null,
        string? username = null,
        string? entityId = null,
        string? entityType = null,
        string? metadata = null,
        string? ipAddress = null,
        string? userAgent = null);

    /// <summary>
    /// Logs a failed operation
    /// </summary>
    Task LogFailureAsync(
        AuditLogCategory category,
        AuditLogAction action,
        string description,
        string errorMessage,
        string? userId = null,
        string? username = null,
        string? entityId = null,
        string? entityType = null,
        string? metadata = null,
        string? ipAddress = null,
        string? userAgent = null);

    /// <summary>
    /// Gets audit logs with filtering and pagination
    /// </summary>
    Task<List<AuditLogDto>> GetLogsAsync(AuditLogFilterRequest filter);

    /// <summary>
    /// Gets audit logs by category
    /// </summary>
    Task<List<AuditLogDto>> GetLogsByCategoryAsync(AuditLogCategory category);

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    Task<List<AuditLogDto>> GetLogsByUserIdAsync(string userId);

    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    Task<List<AuditLogDto>> GetLogsByEntityIdAsync(string entityId);

    /// <summary>
    /// Gets failed audit logs
    /// </summary>
    Task<List<AuditLogDto>> GetFailedLogsAsync();

    /// <summary>
    /// Gets recent audit logs with pagination
    /// </summary>
    Task<List<AuditLogDto>> GetRecentLogsAsync(int pageSize = 50, int pageNumber = 1);

    /// <summary>
    /// Gets count of logs by category
    /// </summary>
    Task<int> GetCountByCategoryAsync(AuditLogCategory category);
}
