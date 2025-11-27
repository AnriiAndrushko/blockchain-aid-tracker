using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository interface for SupplierShipment-specific operations
/// </summary>
public interface ISupplierShipmentRepository : IRepository<SupplierShipment>
{
    /// <summary>
    /// Gets all supplier shipments for a specific shipment
    /// </summary>
    /// <param name="shipmentId">Shipment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of supplier shipments for the shipment</returns>
    Task<List<SupplierShipment>> GetByShipmentIdAsync(string shipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all supplier shipments for a specific supplier
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of supplier shipments for the supplier</returns>
    Task<List<SupplierShipment>> GetBySupplierIdAsync(string supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending payments for a supplier
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending supplier shipments</returns>
    Task<List<SupplierShipment>> GetPendingPaymentsAsync(string supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supplier shipments by payment status
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="status">Payment status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of supplier shipments with specified payment status</returns>
    Task<List<SupplierShipment>> GetByPaymentStatusAsync(string supplierId, SupplierShipmentPaymentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total payment value for a supplier for unreleased payments
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total value of pending/unreleased payments</returns>
    Task<decimal> GetTotalPendingPaymentValueAsync(string supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks payment as released for a supplier shipment
    /// </summary>
    /// <param name="supplierShipmentId">SupplierShipment ID</param>
    /// <param name="transactionReference">Blockchain transaction reference</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated supplier shipment</returns>
    Task<SupplierShipment?> ReleasePaymentAsync(string supplierShipmentId, string transactionReference, CancellationToken cancellationToken = default);
}
