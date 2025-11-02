using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.Shipment;

namespace BlockchainAidTracker.Services.Interfaces;

/// <summary>
/// Service for shipment management with blockchain integration
/// </summary>
public interface IShipmentService
{
    /// <summary>
    /// Creates a new shipment and records it on the blockchain
    /// </summary>
    /// <param name="request">Shipment creation request</param>
    /// <param name="coordinatorId">ID of the coordinator creating the shipment</param>
    /// <returns>Created shipment DTO</returns>
    Task<ShipmentDto> CreateShipmentAsync(CreateShipmentRequest request, string coordinatorId);

    /// <summary>
    /// Gets a shipment by ID
    /// </summary>
    /// <param name="shipmentId">Shipment ID</param>
    /// <returns>Shipment DTO</returns>
    Task<ShipmentDto?> GetShipmentByIdAsync(string shipmentId);

    /// <summary>
    /// Gets all shipments with optional filters
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="recipientId">Optional recipient ID filter</param>
    /// <returns>List of shipment DTOs</returns>
    Task<List<ShipmentDto>> GetShipmentsAsync(ShipmentStatus? status = null, string? recipientId = null);

    /// <summary>
    /// Updates shipment status and records on blockchain
    /// </summary>
    /// <param name="shipmentId">Shipment ID</param>
    /// <param name="newStatus">New status</param>
    /// <param name="updatedBy">ID of user making the update</param>
    /// <returns>Updated shipment DTO</returns>
    Task<ShipmentDto> UpdateShipmentStatusAsync(string shipmentId, ShipmentStatus newStatus, string updatedBy);

    /// <summary>
    /// Confirms delivery of a shipment
    /// </summary>
    /// <param name="shipmentId">Shipment ID</param>
    /// <param name="recipientId">ID of recipient confirming delivery</param>
    /// <returns>Updated shipment DTO</returns>
    Task<ShipmentDto> ConfirmDeliveryAsync(string shipmentId, string recipientId);

    /// <summary>
    /// Gets shipment history from blockchain
    /// </summary>
    /// <param name="shipmentId">Shipment ID</param>
    /// <returns>List of transaction IDs for this shipment</returns>
    Task<List<string>> GetShipmentBlockchainHistoryAsync(string shipmentId);

    /// <summary>
    /// Verifies shipment exists on blockchain
    /// </summary>
    /// <param name="shipmentId">Shipment ID</param>
    /// <returns>True if shipment is verified on blockchain</returns>
    Task<bool> VerifyShipmentOnBlockchainAsync(string shipmentId);
}
