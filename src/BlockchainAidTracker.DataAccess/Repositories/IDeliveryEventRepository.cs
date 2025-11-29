using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository interface for managing delivery events
/// </summary>
public interface IDeliveryEventRepository : IRepository<DeliveryEvent>
{
    /// <summary>
    /// Gets all delivery events for a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <returns>List of DeliveryEvents ordered by timestamp (oldest to newest)</returns>
    Task<List<DeliveryEvent>> GetByShipmentAsync(string shipmentId);

    /// <summary>
    /// Gets all delivery events of a specific type
    /// </summary>
    /// <param name="eventType">The delivery event type to filter by</param>
    /// <returns>List of DeliveryEvents of the specified type</returns>
    Task<List<DeliveryEvent>> GetByEventTypeAsync(DeliveryEventType eventType);

    /// <summary>
    /// Gets the most recent events for a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="count">Number of recent events to retrieve</param>
    /// <returns>List of recent DeliveryEvents (most recent first)</returns>
    Task<List<DeliveryEvent>> GetRecentAsync(string shipmentId, int count);

    /// <summary>
    /// Gets delivery events within a date range for a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="startDate">Start date (inclusive, UTC)</param>
    /// <param name="endDate">End date (inclusive, UTC)</param>
    /// <returns>List of DeliveryEvents within the date range</returns>
    Task<List<DeliveryEvent>> GetByDateRangeAsync(
        string shipmentId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Gets delivery events for a shipment of a specific type
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="eventType">The event type to filter by</param>
    /// <returns>List of DeliveryEvents of the specified type for the shipment</returns>
    Task<List<DeliveryEvent>> GetByShipmentAndTypeAsync(
        string shipmentId,
        DeliveryEventType eventType);

    /// <summary>
    /// Gets the count of delivery events for a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <returns>Total count of delivery events</returns>
    Task<int> GetCountAsync(string shipmentId);
}
