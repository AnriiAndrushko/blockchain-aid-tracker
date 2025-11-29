namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Enumeration of delivery event types that can occur during shipment delivery
/// </summary>
public enum DeliveryEventType
{
    LocationUpdated = 0,
    DeliveryStarted = 1,
    IssueReported = 2,
    IssueResolved = 3,
    Delivered = 4,
    ReceiptConfirmed = 5
}

/// <summary>
/// Represents an event that occurs during the delivery of a shipment
/// Used for tracking delivery history and logistics partner activities
/// </summary>
public class DeliveryEvent
{
    /// <summary>
    /// Unique identifier for the delivery event
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Foreign key to the Shipment entity
    /// </summary>
    public string ShipmentId { get; set; }

    /// <summary>
    /// Type of delivery event
    /// </summary>
    public DeliveryEventType EventType { get; set; }

    /// <summary>
    /// Detailed description of the event
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Timestamp when this event occurred (UTC)
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Foreign key to the User who created this event
    /// </summary>
    public string CreatedByUserId { get; set; }

    /// <summary>
    /// JSON-serialized metadata related to this event
    /// Examples: {"locationId": "..."}, {"issueType": "damage"}
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public DeliveryEvent()
    {
        Id = Guid.NewGuid().ToString();
        Description = string.Empty;
        CreatedTimestamp = DateTime.UtcNow;
        CreatedByUserId = string.Empty;
    }

    /// <summary>
    /// Parameterized constructor for creating a delivery event
    /// </summary>
    public DeliveryEvent(
        string shipmentId,
        DeliveryEventType eventType,
        string description,
        string createdByUserId,
        string? metadata = null)
    {
        Id = Guid.NewGuid().ToString();
        ShipmentId = shipmentId ?? throw new ArgumentNullException(nameof(shipmentId));
        EventType = eventType;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        CreatedByUserId = createdByUserId ?? throw new ArgumentNullException(nameof(createdByUserId));
        Metadata = metadata;
        CreatedTimestamp = DateTime.UtcNow;
    }
}
