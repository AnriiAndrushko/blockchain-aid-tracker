namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Represents an audit log entry for tracking system operations
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Category of the audit log entry
    /// </summary>
    public AuditLogCategory Category { get; set; }

    /// <summary>
    /// Specific action that was performed
    /// </summary>
    public AuditLogAction Action { get; set; }

    /// <summary>
    /// ID of the user who performed the action (nullable for system actions)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Username of the user who performed the action (for quick reference)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// ID of the entity affected by the action (e.g., ShipmentId, ValidatorId)
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Type name of the entity affected (e.g., "Shipment", "User", "Block")
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Description of the action performed
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Additional metadata in JSON format (optional)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// IP address from which the action was performed (if applicable)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string (if applicable)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the action failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when the action occurred (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Default constructor - initializes a new audit log entry
    /// </summary>
    public AuditLog()
    {
        Id = Guid.NewGuid().ToString();
        Description = string.Empty;
        IsSuccess = true;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Parameterized constructor for creating an audit log entry
    /// </summary>
    /// <param name="category">Category of the action</param>
    /// <param name="action">Specific action performed</param>
    /// <param name="description">Description of the action</param>
    /// <param name="userId">ID of the user who performed the action</param>
    /// <param name="username">Username of the user</param>
    /// <param name="entityId">ID of the affected entity</param>
    /// <param name="entityType">Type of the affected entity</param>
    /// <param name="isSuccess">Whether the action was successful</param>
    /// <param name="errorMessage">Error message if failed</param>
    public AuditLog(
        AuditLogCategory category,
        AuditLogAction action,
        string description,
        string? userId = null,
        string? username = null,
        string? entityId = null,
        string? entityType = null,
        bool isSuccess = true,
        string? errorMessage = null)
    {
        Id = Guid.NewGuid().ToString();
        Category = category;
        Action = action;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        UserId = userId;
        Username = username;
        EntityId = entityId;
        EntityType = entityType;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates an audit log entry for a successful operation
    /// </summary>
    public static AuditLog Success(
        AuditLogCategory category,
        AuditLogAction action,
        string description,
        string? userId = null,
        string? username = null,
        string? entityId = null,
        string? entityType = null)
    {
        return new AuditLog(category, action, description, userId, username, entityId, entityType, true, null);
    }

    /// <summary>
    /// Creates an audit log entry for a failed operation
    /// </summary>
    public static AuditLog Failure(
        AuditLogCategory category,
        AuditLogAction action,
        string description,
        string errorMessage,
        string? userId = null,
        string? username = null,
        string? entityId = null,
        string? entityType = null)
    {
        return new AuditLog(category, action, description, userId, username, entityId, entityType, false, errorMessage);
    }
}
