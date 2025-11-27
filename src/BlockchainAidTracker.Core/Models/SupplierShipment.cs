namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Enum for supplier shipment payment status
/// </summary>
public enum SupplierShipmentPaymentStatus
{
    /// <summary>
    /// Payment is pending
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment has been completed
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Payment failed
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Payment is in dispute
    /// </summary>
    Disputed = 3
}

/// <summary>
/// Junction entity linking suppliers to shipments and tracking goods provided
/// </summary>
public class SupplierShipment
{
    /// <summary>
    /// Unique identifier for this supplier-shipment relationship
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Foreign key to Supplier
    /// </summary>
    public string SupplierId { get; set; }

    /// <summary>
    /// Foreign key to Shipment
    /// </summary>
    public string ShipmentId { get; set; }

    /// <summary>
    /// Description of goods provided by this supplier for this shipment
    /// </summary>
    public string GoodsDescription { get; set; }

    /// <summary>
    /// Quantity of goods provided
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measurement (e.g., "kg", "boxes", "units")
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// Value of the goods in the specified currency
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Currency code (USD, EUR, etc.)
    /// </summary>
    public string Currency { get; set; }

    /// <summary>
    /// Timestamp when the goods were provided
    /// </summary>
    public DateTime ProvidedTimestamp { get; set; }

    /// <summary>
    /// Flag indicating whether payment has been released for these goods
    /// </summary>
    public bool PaymentReleased { get; set; }

    /// <summary>
    /// Timestamp when the payment was released (nullable if not yet released)
    /// </summary>
    public DateTime? PaymentReleasedTimestamp { get; set; }

    /// <summary>
    /// Blockchain transaction hash referencing the payment release
    /// </summary>
    public string? PaymentTransactionReference { get; set; }

    /// <summary>
    /// Status of the payment
    /// </summary>
    public SupplierShipmentPaymentStatus PaymentStatus { get; set; }

    /// <summary>
    /// Navigation property for the supplier
    /// </summary>
    public Supplier? Supplier { get; set; }

    /// <summary>
    /// Navigation property for the shipment
    /// </summary>
    public Shipment? Shipment { get; set; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public SupplierShipment()
    {
        Id = Guid.NewGuid().ToString();
        ProvidedTimestamp = DateTime.UtcNow;
        PaymentReleased = false;
        PaymentStatus = SupplierShipmentPaymentStatus.Pending;
    }

    /// <summary>
    /// Parameterized constructor
    /// </summary>
    public SupplierShipment(
        string supplierId,
        string shipmentId,
        string goodsDescription,
        decimal quantity,
        string unit,
        decimal value,
        string currency) : this()
    {
        SupplierId = supplierId ?? throw new ArgumentNullException(nameof(supplierId));
        ShipmentId = shipmentId ?? throw new ArgumentNullException(nameof(shipmentId));
        GoodsDescription = goodsDescription ?? throw new ArgumentNullException(nameof(goodsDescription));
        Quantity = quantity;
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
        Value = value;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    /// <summary>
    /// Marks payment as released
    /// </summary>
    /// <param name="transactionReference">Blockchain transaction hash for the payment</param>
    public void ReleasePayment(string transactionReference)
    {
        PaymentReleased = true;
        PaymentReleasedTimestamp = DateTime.UtcNow;
        PaymentTransactionReference = transactionReference;
        PaymentStatus = SupplierShipmentPaymentStatus.Completed;
    }

    /// <summary>
    /// Marks payment as failed
    /// </summary>
    public void FailPayment()
    {
        PaymentStatus = SupplierShipmentPaymentStatus.Failed;
    }

    /// <summary>
    /// Disputes the payment
    /// </summary>
    public void DisputePayment()
    {
        PaymentStatus = SupplierShipmentPaymentStatus.Disputed;
    }
}
