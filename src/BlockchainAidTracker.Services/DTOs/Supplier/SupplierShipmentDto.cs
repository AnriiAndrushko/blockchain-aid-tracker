using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.Supplier;

/// <summary>
/// Data transfer object for supplier shipment information
/// </summary>
public class SupplierShipmentDto
{
    /// <summary>
    /// Unique identifier for this supplier-shipment relationship
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Description of goods provided
    /// </summary>
    public string GoodsDescription { get; set; } = string.Empty;

    /// <summary>
    /// Quantity of goods provided
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measurement
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Value of the goods
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when goods were provided
    /// </summary>
    public DateTime ProvidedTimestamp { get; set; }

    /// <summary>
    /// Whether payment has been released
    /// </summary>
    public bool PaymentReleased { get; set; }

    /// <summary>
    /// Status of the payment
    /// </summary>
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Shipment ID
    /// </summary>
    public string ShipmentId { get; set; } = string.Empty;

    /// <summary>
    /// Creates a SupplierShipmentDto from a SupplierShipment model
    /// </summary>
    public static SupplierShipmentDto FromSupplierShipment(SupplierShipment supplierShipment)
    {
        return new SupplierShipmentDto
        {
            Id = supplierShipment.Id,
            GoodsDescription = supplierShipment.GoodsDescription,
            Quantity = supplierShipment.Quantity,
            Unit = supplierShipment.Unit,
            Value = supplierShipment.Value,
            Currency = supplierShipment.Currency,
            ProvidedTimestamp = supplierShipment.ProvidedTimestamp,
            PaymentReleased = supplierShipment.PaymentReleased,
            PaymentStatus = supplierShipment.PaymentStatus.ToString(),
            ShipmentId = supplierShipment.ShipmentId
        };
    }
}
