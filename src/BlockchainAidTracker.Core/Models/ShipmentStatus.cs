namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Represents the current status of a shipment in the supply chain
/// </summary>
public enum ShipmentStatus
{
    /// <summary>
    /// Shipment has been created but not yet validated by validators
    /// </summary>
    Created = 0,

    /// <summary>
    /// Shipment has been validated and confirmed by validators
    /// </summary>
    Validated = 1,

    /// <summary>
    /// Shipment is currently in transit to the destination
    /// </summary>
    InTransit = 2,

    /// <summary>
    /// Shipment has been delivered to the destination
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// Delivery has been confirmed by the recipient
    /// </summary>
    Confirmed = 4
}
