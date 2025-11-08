using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.AuditLog;

/// <summary>
/// Data transfer object for audit log entries
/// </summary>
public class AuditLogDto
{
    public string Id { get; set; } = string.Empty;
    public AuditLogCategory Category { get; set; }
    public AuditLogAction Action { get; set; }
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}
