using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.LogisticsPartner;

/// <summary>
/// DTO for delivery event information
/// </summary>
public class DeliveryEventDto
{
    /// <summary>
    /// Event ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Shipment ID
    /// </summary>
    public string ShipmentId { get; set; }

    /// <summary>
    /// Type of delivery event
    /// </summary>
    public DeliveryEventType EventType { get; set; }

    /// <summary>
    /// Event type as string for display
    /// </summary>
    public string EventTypeDisplay => EventType switch
    {
        DeliveryEventType.LocationUpdated => "Location Updated",
        DeliveryEventType.DeliveryStarted => "Delivery Started",
        DeliveryEventType.IssueReported => "Issue Reported",
        DeliveryEventType.IssueResolved => "Issue Resolved",
        DeliveryEventType.Delivered => "Delivered",
        DeliveryEventType.ReceiptConfirmed => "Receipt Confirmed",
        _ => "Unknown"
    };

    /// <summary>
    /// Description of the event
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Timestamp when event occurred
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// User ID who created this event
    /// </summary>
    public string CreatedByUserId { get; set; }

    /// <summary>
    /// JSON metadata (if any)
    /// </summary>
    public string? Metadata { get; set; }
}
