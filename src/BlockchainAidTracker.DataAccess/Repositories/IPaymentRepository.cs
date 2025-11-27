using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository interface for PaymentRecord-specific operations
/// </summary>
public interface IPaymentRepository : IRepository<PaymentRecord>
{
    /// <summary>
    /// Gets all payment records for a specific supplier
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of payment records</returns>
    Task<List<PaymentRecord>> GetBySupplierIdAsync(string supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payment records for a specific shipment
    /// </summary>
    /// <param name="shipmentId">Shipment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of payment records</returns>
    Task<List<PaymentRecord>> GetByShipmentIdAsync(string shipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending payment records
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending payment records</returns>
    Task<List<PaymentRecord>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payment records by status
    /// </summary>
    /// <param name="status">Payment status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of payment records with specified status</returns>
    Task<List<PaymentRecord>> GetByStatusAsync(PaymentRecordStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed payment records that can be retried
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of failed payment records</returns>
    Task<List<PaymentRecord>> GetRetryableFailedPaymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payment records by supplier and status
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="status">Payment status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of payment records</returns>
    Task<List<PaymentRecord>> GetBySupplierAndStatusAsync(string supplierId, PaymentRecordStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total payment value by supplier for a given status
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="status">Payment status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total payment value</returns>
    Task<decimal> GetTotalBySupplierAndStatusAsync(string supplierId, PaymentRecordStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payment records created within a date range
    /// </summary>
    /// <param name="startDate">Start date (UTC)</param>
    /// <param name="endDate">End date (UTC)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of payment records</returns>
    Task<List<PaymentRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
