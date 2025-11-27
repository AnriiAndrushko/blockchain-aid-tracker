using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for Supplier-specific operations
/// </summary>
public class SupplierRepository : Repository<Supplier>, ISupplierRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public SupplierRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Supplier?> GetByCompanyNameAsync(string companyName, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.CompanyName == companyName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Supplier>> GetByVerificationStatusAsync(SupplierVerificationStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.VerificationStatus == status)
            .OrderBy(s => s.CompanyName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Supplier>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.IsActive)
            .OrderBy(s => s.CompanyName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Supplier>> GetVerifiedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.IsActive && s.VerificationStatus == SupplierVerificationStatus.Verified)
            .OrderBy(s => s.CompanyName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Supplier?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SupplierShipment>> GetSupplierShipmentsAsync(string supplierId, CancellationToken cancellationToken = default)
    {
        return await _context.SupplierShipments
            .Where(ss => ss.SupplierId == supplierId)
            .Include(ss => ss.Shipment)
            .OrderByDescending(ss => ss.ProvidedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> CompanyNameExistsAsync(string companyName, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(s => s.CompanyName == companyName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TaxIdExistsAsync(string taxId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(s => s.TaxId == taxId, cancellationToken);
    }
}
