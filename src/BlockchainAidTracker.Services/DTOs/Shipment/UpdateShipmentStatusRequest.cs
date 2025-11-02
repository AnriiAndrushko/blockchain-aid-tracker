using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.Shipment;

/// <summary>
/// Request DTO for updating shipment status
/// </summary>
public class UpdateShipmentStatusRequest
{
    public string ShipmentId { get; set; } = string.Empty;
    public ShipmentStatus NewStatus { get; set; }
    public string? Notes { get; set; }
}
