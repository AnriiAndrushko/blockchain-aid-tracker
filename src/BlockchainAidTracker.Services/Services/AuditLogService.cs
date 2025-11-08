using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.AuditLog;
using BlockchainAidTracker.Services.Interfaces;

namespace BlockchainAidTracker.Services.Services;

/// <summary>
/// Implementation of audit logging service
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
    }

    public async Task LogAsync(
        AuditLogCategory category,
        AuditLogAction action,
        string description,
        string? userId = null,
        string? username = null,
        string? entityId = null,
        string? entityType = null,
        string? metadata = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var auditLog = new AuditLog
        {
            Category = category,
            Action = action,
            Description = description,
            UserId = userId,
            Username = username,
            EntityId = entityId,
            EntityType = entityType,
            Metadata = metadata,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsSuccess = true,
            Timestamp = DateTime.UtcNow
        };

        await _auditLogRepository.AddAsync(auditLog);
    }

    public async Task LogFailureAsync(
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
        string? userAgent = null)
    {
        var auditLog = new AuditLog
        {
            Category = category,
            Action = action,
            Description = description,
            UserId = userId,
            Username = username,
            EntityId = entityId,
            EntityType = entityType,
            Metadata = metadata,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };

        await _auditLogRepository.AddAsync(auditLog);
    }

    public async Task<List<AuditLogDto>> GetLogsAsync(AuditLogFilterRequest filter)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        var logs = await _auditLogRepository.GetFilteredLogsAsync(
            filter.Category,
            filter.Action,
            filter.UserId,
            filter.EntityId,
            filter.IsSuccess,
            filter.StartDate,
            filter.EndDate,
            filter.PageSize,
            filter.PageNumber);

        return logs.Select(MapToDto).ToList();
    }

    public async Task<List<AuditLogDto>> GetLogsByCategoryAsync(AuditLogCategory category)
    {
        var logs = await _auditLogRepository.GetByCategoryAsync(category);
        return logs.Select(MapToDto).ToList();
    }

    public async Task<List<AuditLogDto>> GetLogsByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var logs = await _auditLogRepository.GetByUserIdAsync(userId);
        return logs.Select(MapToDto).ToList();
    }

    public async Task<List<AuditLogDto>> GetLogsByEntityIdAsync(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        var logs = await _auditLogRepository.GetByEntityIdAsync(entityId);
        return logs.Select(MapToDto).ToList();
    }

    public async Task<List<AuditLogDto>> GetFailedLogsAsync()
    {
        var logs = await _auditLogRepository.GetFailedLogsAsync();
        return logs.Select(MapToDto).ToList();
    }

    public async Task<List<AuditLogDto>> GetRecentLogsAsync(int pageSize = 50, int pageNumber = 1)
    {
        var logs = await _auditLogRepository.GetRecentLogsAsync(pageSize, pageNumber);
        return logs.Select(MapToDto).ToList();
    }

    public async Task<int> GetCountByCategoryAsync(AuditLogCategory category)
    {
        return await _auditLogRepository.GetCountByCategoryAsync(category);
    }

    private static AuditLogDto MapToDto(AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            Category = auditLog.Category,
            Action = auditLog.Action,
            UserId = auditLog.UserId,
            Username = auditLog.Username,
            EntityId = auditLog.EntityId,
            EntityType = auditLog.EntityType,
            Description = auditLog.Description,
            Metadata = auditLog.Metadata,
            IpAddress = auditLog.IpAddress,
            UserAgent = auditLog.UserAgent,
            IsSuccess = auditLog.IsSuccess,
            ErrorMessage = auditLog.ErrorMessage,
            Timestamp = auditLog.Timestamp
        };
    }
}
