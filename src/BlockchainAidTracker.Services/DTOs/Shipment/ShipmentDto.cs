using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.Shipment;

/// <summary>
/// DTO for shipment information
/// </summary>
public class ShipmentDto
{
    public string Id { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public string? DonorId { get; set; }
    public string? LogisticsPartnerId { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public ShipmentStatus Status { get; set; }
    public string QrCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ShipmentItemDto> Items { get; set; } = new();
    public List<string> TransactionIds { get; set; } = new();
}
