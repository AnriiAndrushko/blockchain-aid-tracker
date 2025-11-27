namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Enumeration of user roles in the blockchain aid tracking system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Recipient of humanitarian aid
    /// </summary>
    Recipient = 0,

    /// <summary>
    /// Donor who funds humanitarian aid shipments
    /// </summary>
    Donor = 1,

    /// <summary>
    /// Coordinator who creates and manages shipments
    /// </summary>
    Coordinator = 2,

    /// <summary>
    /// Logistics partner who handles transportation
    /// </summary>
    LogisticsPartner = 3,

    /// <summary>
    /// Validator node operator in the PoA consensus
    /// </summary>
    Validator = 4,

    /// <summary>
    /// System administrator with full access
    /// </summary>
    Administrator = 5,

    /// <summary>
    /// Customer/Supplier who provides goods and receives automatic payment
    /// </summary>
    Customer = 6
}
