using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for shipment location records
/// </summary>
public class ShipmentLocationRepository : Repository<ShipmentLocation>, IShipmentLocationRepository
{
    private readonly ApplicationDbContext _context;

    public ShipmentLocationRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<ShipmentLocation?> GetLatestAsync(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        return await _context.ShipmentLocations
            .Where(sl => sl.ShipmentId == shipmentId)
            .OrderByDescending(sl => sl.CreatedTimestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ShipmentLocation>> GetHistoryAsync(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        return await _context.ShipmentLocations
            .Where(sl => sl.ShipmentId == shipmentId)
            .OrderBy(sl => sl.CreatedTimestamp)
            .ToListAsync();
    }

    public async Task<List<ShipmentLocation>> GetHistoryByDateRangeAsync(
        string shipmentId,
        DateTime startDate,
        DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        if (startDate > endDate)
            throw new ArgumentException("Start date must be before or equal to end date");

        return await _context.ShipmentLocations
            .Where(sl => sl.ShipmentId == shipmentId &&
                         sl.CreatedTimestamp >= startDate &&
                         sl.CreatedTimestamp <= endDate)
            .OrderBy(sl => sl.CreatedTimestamp)
            .ToListAsync();
    }

    public async Task<List<ShipmentLocation>> GetPaginatedAsync(
        string shipmentId,
        int pageNumber,
        int pageSize)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        if (pageNumber < 1)
            throw new ArgumentException("Page number must be 1 or greater", nameof(pageNumber));

        if (pageSize < 1)
            throw new ArgumentException("Page size must be 1 or greater", nameof(pageSize));

        var skip = (pageNumber - 1) * pageSize;

        return await _context.ShipmentLocations
            .Where(sl => sl.ShipmentId == shipmentId)
            .OrderByDescending(sl => sl.CreatedTimestamp)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        return await _context.ShipmentLocations
            .CountAsync(sl => sl.ShipmentId == shipmentId);
    }
}
