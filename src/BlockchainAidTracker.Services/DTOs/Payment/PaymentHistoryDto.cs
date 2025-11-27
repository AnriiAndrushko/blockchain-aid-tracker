using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.Payment;

/// <summary>
/// Data transfer object for supplier payment history
/// </summary>
public class PaymentHistoryDto
{
    /// <summary>
    /// Unique identifier for this payment record
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Amount paid
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
    /// Payment status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when payment was initiated
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Timestamp when payment was completed (if completed)
    /// </summary>
    public DateTime? CompletedTimestamp { get; set; }

    /// <summary>
    /// Shipment ID related to this payment
    /// </summary>
    public string ShipmentId { get; set; } = string.Empty;

    /// <summary>
    /// Number of payment attempts
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Creates a PaymentHistoryDto from a PaymentRecord model
    /// </summary>
    public static PaymentHistoryDto FromPaymentRecord(PaymentRecord payment)
    {
        return new PaymentHistoryDto
        {
            Id = payment.Id,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod.ToString(),
            Status = payment.Status.ToString(),
            CreatedTimestamp = payment.CreatedTimestamp,
            CompletedTimestamp = payment.CompletedTimestamp,
            ShipmentId = payment.ShipmentId,
            AttemptCount = payment.AttemptCount
        };
    }
}
