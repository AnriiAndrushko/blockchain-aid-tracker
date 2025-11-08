using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for AuditLog-specific operations
/// </summary>
public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public AuditLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<List<AuditLog>> GetByCategoryAsync(AuditLogCategory category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Category == category)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<AuditLog>> GetByActionAsync(AuditLogAction action, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Action == action)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<AuditLog>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<AuditLog>> GetByEntityIdAsync(string entityId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<AuditLog>> GetByEntityTypeAsync(string entityType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.EntityType == entityType)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<AuditLog>> GetFailedLogsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => !a.IsSuccess)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<AuditLog>> GetRecentLogsAsync(int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetCountByCategoryAsync(AuditLogCategory category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Category == category)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<AuditLog>> GetFilteredLogsAsync(
        AuditLogCategory? category = null,
        AuditLogAction? action = null,
        string? userId = null,
        string? entityId = null,
        bool? isSuccess = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageSize = 50,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (category.HasValue)
            query = query.Where(a => a.Category == category.Value);

        if (action.HasValue)
            query = query.Where(a => a.Action == action.Value);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(a => a.UserId == userId);

        if (!string.IsNullOrEmpty(entityId))
            query = query.Where(a => a.EntityId == entityId);

        if (isSuccess.HasValue)
            query = query.Where(a => a.IsSuccess == isSuccess.Value);

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
