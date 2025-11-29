using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for delivery event records
/// </summary>
public class DeliveryEventRepository : Repository<DeliveryEvent>, IDeliveryEventRepository
{
    private readonly ApplicationDbContext _context;

    public DeliveryEventRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<DeliveryEvent>> GetByShipmentAsync(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        return await _context.DeliveryEvents
            .Where(de => de.ShipmentId == shipmentId)
            .OrderBy(de => de.CreatedTimestamp)
            .ToListAsync();
    }

    public async Task<List<DeliveryEvent>> GetByEventTypeAsync(DeliveryEventType eventType)
    {
        return await _context.DeliveryEvents
            .Where(de => de.EventType == eventType)
            .OrderByDescending(de => de.CreatedTimestamp)
            .ToListAsync();
    }

    public async Task<List<DeliveryEvent>> GetRecentAsync(string shipmentId, int count)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        if (count < 1)
            throw new ArgumentException("Count must be 1 or greater", nameof(count));

        return await _context.DeliveryEvents
            .Where(de => de.ShipmentId == shipmentId)
            .OrderByDescending(de => de.CreatedTimestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<DeliveryEvent>> GetByDateRangeAsync(
        string shipmentId,
        DateTime startDate,
        DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        if (startDate > endDate)
            throw new ArgumentException("Start date must be before or equal to end date");

        return await _context.DeliveryEvents
            .Where(de => de.ShipmentId == shipmentId &&
                         de.CreatedTimestamp >= startDate &&
                         de.CreatedTimestamp <= endDate)
            .OrderBy(de => de.CreatedTimestamp)
            .ToListAsync();
    }

    public async Task<List<DeliveryEvent>> GetByShipmentAndTypeAsync(
        string shipmentId,
        DeliveryEventType eventType)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        return await _context.DeliveryEvents
            .Where(de => de.ShipmentId == shipmentId && de.EventType == eventType)
            .OrderBy(de => de.CreatedTimestamp)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        return await _context.DeliveryEvents
            .CountAsync(de => de.ShipmentId == shipmentId);
    }
}
