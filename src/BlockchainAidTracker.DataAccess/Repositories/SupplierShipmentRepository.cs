using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for SupplierShipment-specific operations
/// </summary>
public class SupplierShipmentRepository : Repository<SupplierShipment>, ISupplierShipmentRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public SupplierShipmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<List<SupplierShipment>> GetByShipmentIdAsync(string shipmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ss => ss.ShipmentId == shipmentId)
            .Include(ss => ss.Supplier)
            .OrderByDescending(ss => ss.ProvidedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SupplierShipment>> GetBySupplierIdAsync(string supplierId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ss => ss.Shipment)
            .Where(ss => ss.SupplierId == supplierId)
            .OrderByDescending(ss => ss.ProvidedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SupplierShipment>> GetPendingPaymentsAsync(string supplierId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ss => ss.Shipment)
            .Where(ss => ss.SupplierId == supplierId && !ss.PaymentReleased && ss.PaymentStatus == SupplierShipmentPaymentStatus.Pending)
            .OrderByDescending(ss => ss.ProvidedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SupplierShipment>> GetByPaymentStatusAsync(string supplierId, SupplierShipmentPaymentStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ss => ss.Shipment)
            .Where(ss => ss.SupplierId == supplierId && ss.PaymentStatus == status)
            .OrderByDescending(ss => ss.ProvidedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalPendingPaymentValueAsync(string supplierId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ss => ss.SupplierId == supplierId && !ss.PaymentReleased && ss.PaymentStatus == SupplierShipmentPaymentStatus.Pending)
            .SumAsync(ss => ss.Value, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SupplierShipment?> ReleasePaymentAsync(string supplierShipmentId, string transactionReference, CancellationToken cancellationToken = default)
    {
        var supplierShipment = await _dbSet.FirstOrDefaultAsync(ss => ss.Id == supplierShipmentId, cancellationToken);
        if (supplierShipment == null)
            return null;

        supplierShipment.ReleasePayment(transactionReference);
        _dbSet.Update(supplierShipment);
        await _context.SaveChangesAsync(cancellationToken);

        return supplierShipment;
    }
}
