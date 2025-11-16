using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository interface for Shipment-specific operations
/// </summary>
public interface IShipmentRepository : IRepository<Shipment>
{
    /// <summary>
    /// Gets a shipment by ID with all related items included
    /// </summary>
    /// <param name="id">Shipment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Shipment with items, or null if not found</returns>
    Task<Shipment?> GetByIdWithItemsAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all shipments with their items
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of shipments with items</returns>
    Task<List<Shipment>> GetAllWithItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shipments by status
    /// </summary>
    /// <param name="status">Shipment status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of shipments with the specified status</returns>
    Task<List<Shipment>> GetByStatusAsync(ShipmentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shipments by assigned recipient
    /// </summary>
    /// <param name="recipientPublicKey">Recipient's public key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of shipments assigned to the recipient</returns>
    Task<List<Shipment>> GetByRecipientAsync(string recipientPublicKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shipments by coordinator
    /// </summary>
    /// <param name="coordinatorPublicKey">Coordinator's public key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of shipments created by the coordinator</returns>
    Task<List<Shipment>> GetByCoordinatorAsync(string coordinatorPublicKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shipments by donor
    /// </summary>
    /// <param name="donorPublicKey">Donor's public key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of shipments funded by the donor</returns>
    Task<List<Shipment>> GetByDonorAsync(string donorPublicKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shipments by donor ID
    /// </summary>
    /// <param name="donorId">Donor's user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of shipments funded by the donor</returns>
    Task<List<Shipment>> GetByDonorIdAsync(string donorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shipments by logistics partner ID
    /// </summary>
    /// <param name="logisticsPartnerId">Logistics partner's user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of shipments assigned to the logistics partner</returns>
    Task<List<Shipment>> GetByLogisticsPartnerIdAsync(string logisticsPartnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a shipment by QR code data
    /// </summary>
    /// <param name="qrCodeData">QR code data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Shipment matching the QR code, or null if not found</returns>
    Task<Shipment?> GetByQrCodeAsync(string qrCodeData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shipments created within a date range
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of shipments created within the date range</returns>
    Task<List<Shipment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
