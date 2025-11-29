using BlockchainAidTracker.Services.DTOs.LogisticsPartner;
using BlockchainAidTracker.Services.DTOs.Shipment;

namespace BlockchainAidTracker.Services;

/// <summary>
/// Service interface for LogisticsPartner operations including location tracking and delivery events
/// </summary>
public interface ILogisticsPartnerService
{
    /// <summary>
    /// Gets assigned shipments for a logistics partner
    /// </summary>
    /// <param name="userId">The logistics partner user ID</param>
    /// <param name="status">Optional shipment status filter</param>
    /// <returns>List of shipment DTOs assigned to this partner</returns>
    Task<List<ShipmentDto>> GetAssignedShipmentsAsync(string userId, string? status = null);

    /// <summary>
    /// Gets the current location of a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <returns>Current ShipmentLocation or null if not found</returns>
    Task<ShipmentLocationDto?> GetShipmentLocationAsync(string shipmentId);

    /// <summary>
    /// Updates the location of a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="userId">The user ID updating the location</param>
    /// <param name="request">Location update request</param>
    /// <returns>Updated ShipmentLocation DTO</returns>
    Task<ShipmentLocationDto> UpdateLocationAsync(
        string shipmentId,
        string userId,
        UpdateLocationRequest request);

    /// <summary>
    /// Confirms that delivery has started for a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="userId">The user ID confirming delivery start</param>
    /// <returns>Delivery event DTO for the confirmation</returns>
    Task<DeliveryEventDto> ConfirmDeliveryInitiationAsync(string shipmentId, string userId);

    /// <summary>
    /// Gets the delivery history for a shipment including all location and event updates
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <returns>List of delivery events chronologically ordered</returns>
    Task<List<DeliveryEventDto>> GetDeliveryHistoryAsync(string shipmentId);

    /// <summary>
    /// Gets location history for a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="limit">Maximum number of recent locations to return</param>
    /// <returns>List of location history DTOs</returns>
    Task<List<ShipmentLocationDto>> GetLocationHistoryAsync(string shipmentId, int limit = 10);

    /// <summary>
    /// Reports a delivery issue
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="userId">The user ID reporting the issue</param>
    /// <param name="request">Issue report request</param>
    /// <returns>Delivery event DTO for the issue report</returns>
    Task<DeliveryEventDto> ReportDeliveryIssueAsync(
        string shipmentId,
        string userId,
        ReportIssueRequest request);

    /// <summary>
    /// Confirms final receipt of a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="userId">The user ID confirming receipt</param>
    /// <returns>Delivery event DTO for the receipt confirmation</returns>
    Task<DeliveryEventDto> ConfirmReceiptAsync(string shipmentId, string userId);
}
