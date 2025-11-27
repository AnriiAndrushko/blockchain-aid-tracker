using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.Payment;

namespace BlockchainAidTracker.Services.Interfaces;

/// <summary>
/// Interface for automated payment processing service
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Calculates total payment amount for a supplier's goods on a shipment
    /// </summary>
    /// <param name="shipmentId">Shipment ID</param>
    /// <param name="supplierId">Supplier ID</param>
    /// <returns>Total payment amount in the currency of the goods</returns>
    Task<decimal> CalculatePaymentAmountAsync(string shipmentId, string supplierId);

    /// <summary>
    /// Initiates payment for a supplier's goods
    /// </summary>
    /// <param name="shipmentId">Shipment ID</param>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="paymentMethod">Payment method to use</param>
    /// <returns>Created payment record DTO</returns>
    Task<PaymentDto> InitiatePaymentAsync(string shipmentId, string supplierId, PaymentMethod paymentMethod);

    /// <summary>
    /// Completes a payment record after successful processing
    /// </summary>
    /// <param name="paymentId">Payment record ID</param>
    /// <param name="externalReference">External payment reference (optional)</param>
    /// <param name="transactionHash">Blockchain transaction hash (optional)</param>
    /// <returns>Updated payment record DTO</returns>
    Task<PaymentDto> CompletePaymentAsync(string paymentId, string? externalReference = null, string? transactionHash = null);

    /// <summary>
    /// Marks a payment as failed
    /// </summary>
    /// <param name="paymentId">Payment record ID</param>
    /// <param name="reason">Failure reason</param>
    /// <returns>Updated payment record DTO</returns>
    Task<PaymentDto> FailPaymentAsync(string paymentId, string reason);

    /// <summary>
    /// Retries a failed payment
    /// </summary>
    /// <param name="paymentId">Payment record ID</param>
    /// <returns>Updated payment record DTO</returns>
    Task<PaymentDto> RetryPaymentAsync(string paymentId);

    /// <summary>
    /// Gets a payment record by ID
    /// </summary>
    /// <param name="paymentId">Payment record ID</param>
    /// <returns>Payment record DTO if found, null otherwise</returns>
    Task<PaymentDto?> GetPaymentByIdAsync(string paymentId);

    /// <summary>
    /// Gets all pending payments
    /// </summary>
    /// <returns>List of pending payment DTOs</returns>
    Task<List<PaymentDto>> GetPendingPaymentsAsync();

    /// <summary>
    /// Gets all failed payments that can be retried
    /// </summary>
    /// <returns>List of retryable payment DTOs</returns>
    Task<List<PaymentDto>> GetRetryablePaymentsAsync();

    /// <summary>
    /// Gets payment history for a supplier
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <returns>List of payment DTOs for the supplier</returns>
    Task<List<PaymentDto>> GetSupplierPaymentsAsync(string supplierId);

    /// <summary>
    /// Gets payment records for a shipment
    /// </summary>
    /// <param name="shipmentId">Shipment ID</param>
    /// <returns>List of payment DTOs for the shipment</returns>
    Task<List<PaymentDto>> GetShipmentPaymentsAsync(string shipmentId);

    /// <summary>
    /// Checks if a supplier is eligible for payment
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <returns>True if supplier is verified and active, false otherwise</returns>
    Task<bool> IsSupplierEligibleForPaymentAsync(string supplierId);

    /// <summary>
    /// Gets total earned amount for a supplier
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <returns>Total earned amount in default currency</returns>
    Task<decimal> GetSupplierTotalEarnedAsync(string supplierId);
}
