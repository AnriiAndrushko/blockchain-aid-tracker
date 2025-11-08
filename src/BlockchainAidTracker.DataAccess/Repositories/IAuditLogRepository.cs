using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository interface for AuditLog-specific operations
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// Gets audit logs by category
    /// </summary>
    /// <param name="category">Category to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs in the specified category</returns>
    Task<List<AuditLog>> GetByCategoryAsync(AuditLogCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by action
    /// </summary>
    /// <param name="action">Action to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs for the specified action</returns>
    Task<List<AuditLog>> GetByActionAsync(AuditLogAction action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs for the user</returns>
    Task<List<AuditLog>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    /// <param name="entityId">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs for the entity</returns>
    Task<List<AuditLog>> GetByEntityIdAsync(string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific entity type
    /// </summary>
    /// <param name="entityType">Entity type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs for the entity type</returns>
    Task<List<AuditLog>> GetByEntityTypeAsync(string entityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs within a time range
    /// </summary>
    /// <param name="startDate">Start date (UTC)</param>
    /// <param name="endDate">End date (UTC)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs within the time range</returns>
    Task<List<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed audit logs
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of failed audit logs</returns>
    Task<List<AuditLog>> GetFailedLogsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent audit logs with pagination
    /// </summary>
    /// <param name="pageSize">Number of logs to retrieve</param>
    /// <param name="pageNumber">Page number (1-indexed)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent audit logs</returns>
    Task<List<AuditLog>> GetRecentLogsAsync(int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of audit logs by category
    /// </summary>
    /// <param name="category">Category to count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of logs in category</returns>
    Task<int> GetCountByCategoryAsync(AuditLogCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs with advanced filtering
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <param name="action">Optional action filter</param>
    /// <param name="userId">Optional user ID filter</param>
    /// <param name="entityId">Optional entity ID filter</param>
    /// <param name="isSuccess">Optional success status filter</param>
    /// <param name="startDate">Optional start date</param>
    /// <param name="endDate">Optional end date</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="pageNumber">Page number (1-indexed)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered list of audit logs</returns>
    Task<List<AuditLog>> GetFilteredLogsAsync(
        AuditLogCategory? category = null,
        AuditLogAction? action = null,
        string? userId = null,
        string? entityId = null,
        bool? isSuccess = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageSize = 50,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);
}
