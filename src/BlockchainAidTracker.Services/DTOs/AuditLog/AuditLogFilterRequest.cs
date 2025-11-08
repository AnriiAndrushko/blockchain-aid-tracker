using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.AuditLog;

/// <summary>
/// Request for filtering audit logs
/// </summary>
public class AuditLogFilterRequest
{
    public AuditLogCategory? Category { get; set; }
    public AuditLogAction? Action { get; set; }
    public string? UserId { get; set; }
    public string? EntityId { get; set; }
    public bool? IsSuccess { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageSize { get; set; } = 50;
    public int PageNumber { get; set; } = 1;
}
