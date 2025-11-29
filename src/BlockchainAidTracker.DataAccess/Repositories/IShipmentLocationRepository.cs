using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository interface for managing shipment location records
/// </summary>
public interface IShipmentLocationRepository : IRepository<ShipmentLocation>
{
    /// <summary>
    /// Gets the most recent location for a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <returns>The latest ShipmentLocation or null if not found</returns>
    Task<ShipmentLocation?> GetLatestAsync(string shipmentId);

    /// <summary>
    /// Gets all location history for a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <returns>List of ShipmentLocations ordered by timestamp (oldest to newest)</returns>
    Task<List<ShipmentLocation>> GetHistoryAsync(string shipmentId);

    /// <summary>
    /// Gets location history within a date range
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="startDate">Start date (inclusive, UTC)</param>
    /// <param name="endDate">End date (inclusive, UTC)</param>
    /// <returns>List of ShipmentLocations within the date range</returns>
    Task<List<ShipmentLocation>> GetHistoryByDateRangeAsync(
        string shipmentId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Gets all locations for a shipment with pagination
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>List of ShipmentLocations for the page</returns>
    Task<List<ShipmentLocation>> GetPaginatedAsync(
        string shipmentId,
        int pageNumber,
        int pageSize);

    /// <summary>
    /// Gets the total count of locations for a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <returns>Total count of location records</returns>
    Task<int> GetCountAsync(string shipmentId);
}
