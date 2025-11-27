using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for PaymentRecord-specific operations
/// </summary>
public class PaymentRepository : Repository<PaymentRecord>, IPaymentRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public PaymentRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<List<PaymentRecord>> GetBySupplierIdAsync(string supplierId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.SupplierId == supplierId)
            .OrderByDescending(p => p.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<PaymentRecord>> GetByShipmentIdAsync(string shipmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ShipmentId == shipmentId)
            .OrderByDescending(p => p.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<PaymentRecord>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Status == PaymentRecordStatus.Initiated)
            .OrderByDescending(p => p.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<PaymentRecord>> GetByStatusAsync(PaymentRecordStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<PaymentRecord>> GetRetryableFailedPaymentsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Status == PaymentRecordStatus.Failed && p.AttemptCount < 3)
            .OrderBy(p => p.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<PaymentRecord>> GetBySupplierAndStatusAsync(string supplierId, PaymentRecordStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.SupplierId == supplierId && p.Status == status)
            .OrderByDescending(p => p.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalBySupplierAndStatusAsync(string supplierId, PaymentRecordStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.SupplierId == supplierId && p.Status == status)
            .SumAsync(p => p.Amount, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<PaymentRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.CreatedTimestamp >= startDate && p.CreatedTimestamp <= endDate)
            .OrderByDescending(p => p.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }
}
