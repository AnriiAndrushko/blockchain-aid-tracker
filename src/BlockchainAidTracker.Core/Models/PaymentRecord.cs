namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Enum for payment record status
/// </summary>
public enum PaymentRecordStatus
{
    /// <summary>
    /// Payment processing has been initiated
    /// </summary>
    Initiated = 0,

    /// <summary>
    /// Payment has been completed
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Payment failed to process
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Payment is being retried after initial failure
    /// </summary>
    Retrying = 3,

    /// <summary>
    /// Payment has been reversed/refunded
    /// </summary>
    Reversed = 4
}

/// <summary>
/// Enum for payment method
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Payment via bank transfer (SEPA/ACH)
    /// </summary>
    BankTransfer = 0,

    /// <summary>
    /// Payment via blockchain token transfer
    /// </summary>
    BlockchainToken = 1,

    /// <summary>
    /// Payment via cryptocurrency
    /// </summary>
    Cryptocurrency = 2,

    /// <summary>
    /// Payment via other method
    /// </summary>
    Other = 3
}

/// <summary>
/// Represents a payment record for supplier goods
/// Tracks automatic payment processing after shipment completion
/// </summary>
public class PaymentRecord
{
    /// <summary>
    /// Unique identifier for this payment record
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
    /// Amount to be paid
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (USD, EUR, etc.)
    /// </summary>
    public string Currency { get; set; }

    /// <summary>
    /// Payment method used
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Current status of the payment
    /// </summary>
    public PaymentRecordStatus Status { get; set; }

    /// <summary>
    /// Blockchain transaction hash for the payment (for token transfers)
    /// </summary>
    public string? BlockchainTransactionHash { get; set; }

    /// <summary>
    /// Timestamp when the payment was initiated (UTC)
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Timestamp when the payment was completed (UTC, nullable)
    /// </summary>
    public DateTime? CompletedTimestamp { get; set; }

    /// <summary>
    /// Reason for failure if payment failed (nullable)
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Number of payment attempts made
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// External payment reference (e.g., bank transaction ID)
    /// </summary>
    public string? ExternalPaymentReference { get; set; }

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
    public PaymentRecord()
    {
        Id = Guid.NewGuid().ToString();
        CreatedTimestamp = DateTime.UtcNow;
        Status = PaymentRecordStatus.Initiated;
        AttemptCount = 1;
    }

    /// <summary>
    /// Parameterized constructor
    /// </summary>
    public PaymentRecord(
        string supplierId,
        string shipmentId,
        decimal amount,
        string currency,
        PaymentMethod paymentMethod) : this()
    {
        SupplierId = supplierId ?? throw new ArgumentNullException(nameof(supplierId));
        ShipmentId = shipmentId ?? throw new ArgumentNullException(nameof(shipmentId));
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        PaymentMethod = paymentMethod;
    }

    /// <summary>
    /// Marks the payment as completed
    /// </summary>
    /// <param name="externalReference">External payment reference (optional)</param>
    /// <param name="transactionHash">Blockchain transaction hash (optional)</param>
    public void Complete(string? externalReference = null, string? transactionHash = null)
    {
        Status = PaymentRecordStatus.Completed;
        CompletedTimestamp = DateTime.UtcNow;
        ExternalPaymentReference = externalReference;
        BlockchainTransactionHash = transactionHash;
    }

    /// <summary>
    /// Marks the payment as failed with a reason
    /// </summary>
    /// <param name="reason">Reason for failure</param>
    public void MarkAsFailed(string reason)
    {
        Status = PaymentRecordStatus.Failed;
        FailureReason = reason;
    }

    /// <summary>
    /// Marks the payment as being retried
    /// </summary>
    public void MarkAsRetrying()
    {
        Status = PaymentRecordStatus.Retrying;
        AttemptCount++;
    }

    /// <summary>
    /// Reverses/refunds the payment
    /// </summary>
    public void Reverse()
    {
        Status = PaymentRecordStatus.Reversed;
    }
}
