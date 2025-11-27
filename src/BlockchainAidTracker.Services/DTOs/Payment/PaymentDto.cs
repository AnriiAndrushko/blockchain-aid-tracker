using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.Payment;

/// <summary>
/// Data transfer object for payment record information
/// </summary>
public class PaymentDto
{
    /// <summary>
    /// Unique identifier for this payment record
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Supplier ID
    /// </summary>
    public string SupplierId { get; set; } = string.Empty;

    /// <summary>
    /// Shipment ID
    /// </summary>
    public string ShipmentId { get; set; } = string.Empty;

    /// <summary>
    /// Amount to be paid
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Payment method
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the payment
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Blockchain transaction hash (if applicable)
    /// </summary>
    public string? BlockchainTransactionHash { get; set; }

    /// <summary>
    /// Timestamp when the payment was initiated
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Timestamp when the payment was completed (if completed)
    /// </summary>
    public DateTime? CompletedTimestamp { get; set; }

    /// <summary>
    /// Reason for failure (if failed)
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Number of payment attempts made
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// External payment reference
    /// </summary>
    public string? ExternalPaymentReference { get; set; }

    /// <summary>
    /// Creates a PaymentDto from a PaymentRecord model
    /// </summary>
    public static PaymentDto FromPaymentRecord(PaymentRecord payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            SupplierId = payment.SupplierId,
            ShipmentId = payment.ShipmentId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod.ToString(),
            Status = payment.Status.ToString(),
            BlockchainTransactionHash = payment.BlockchainTransactionHash,
            CreatedTimestamp = payment.CreatedTimestamp,
            CompletedTimestamp = payment.CompletedTimestamp,
            FailureReason = payment.FailureReason,
            AttemptCount = payment.AttemptCount,
            ExternalPaymentReference = payment.ExternalPaymentReference
        };
    }
}
