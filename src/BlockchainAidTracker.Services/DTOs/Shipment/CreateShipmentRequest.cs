using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.Shipment;

/// <summary>
/// Request DTO for creating a new shipment
/// </summary>
public class CreateShipmentRequest
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public DateTime ExpectedDeliveryDate { get; set; }
    public List<ShipmentItemDto> Items { get; set; } = new();
    public string? DonorId { get; set; }
    public string? LogisticsPartnerId { get; set; }
}

/// <summary>
/// DTO for shipment item
/// </summary>
public class ShipmentItemDto
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
