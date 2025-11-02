using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for Shipment-specific operations
/// </summary>
public class ShipmentRepository : Repository<Shipment>, IShipmentRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public ShipmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Shipment?> GetByIdWithItemsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Shipment>> GetAllWithItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Items)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Shipment>> GetByStatusAsync(ShipmentStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Items)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Shipment>> GetByRecipientAsync(string recipientPublicKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Items)
            .Where(s => s.AssignedRecipient == recipientPublicKey)
            .OrderByDescending(s => s.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Shipment>> GetByCoordinatorAsync(string coordinatorPublicKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Items)
            .Where(s => s.CoordinatorPublicKey == coordinatorPublicKey)
            .OrderByDescending(s => s.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Shipment>> GetByDonorAsync(string donorPublicKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Items)
            .Where(s => s.DonorPublicKey == donorPublicKey)
            .OrderByDescending(s => s.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Shipment?> GetByQrCodeAsync(string qrCodeData, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.QrCodeData == qrCodeData, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Shipment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Items)
            .Where(s => s.CreatedTimestamp >= startDate && s.CreatedTimestamp <= endDate)
            .OrderByDescending(s => s.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }
}
