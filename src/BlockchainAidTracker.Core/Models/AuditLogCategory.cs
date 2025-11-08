namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Represents the category of an audit log entry
/// </summary>
public enum AuditLogCategory
{
    /// <summary>
    /// Authentication-related operations (login, logout, registration)
    /// </summary>
    Authentication,

    /// <summary>
    /// User management operations (profile updates, role changes)
    /// </summary>
    UserManagement,

    /// <summary>
    /// Shipment-related operations (creation, status updates)
    /// </summary>
    Shipment,

    /// <summary>
    /// Blockchain operations (block creation, transaction addition)
    /// </summary>
    Blockchain,

    /// <summary>
    /// Validator node operations (registration, activation)
    /// </summary>
    Validator,

    /// <summary>
    /// Smart contract operations (deployment, execution)
    /// </summary>
    SmartContract
}
