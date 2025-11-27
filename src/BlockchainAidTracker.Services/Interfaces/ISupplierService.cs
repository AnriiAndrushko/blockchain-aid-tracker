using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.Payment;
using BlockchainAidTracker.Services.DTOs.Supplier;

namespace BlockchainAidTracker.Services.Interfaces;

/// <summary>
/// Interface for supplier management service
/// </summary>
public interface ISupplierService
{
    /// <summary>
    /// Registers a new supplier
    /// </summary>
    /// <param name="userId">User ID of the customer registering as supplier</param>
    /// <param name="request">Supplier registration request</param>
    /// <param name="keyManagementService">Key management service for encrypting bank details</param>
    /// <returns>Created supplier DTO</returns>
    Task<SupplierDto> RegisterSupplierAsync(string userId, CreateSupplierRequest request, IKeyManagementService keyManagementService);

    /// <summary>
    /// Gets a supplier by ID
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <returns>Supplier DTO if found, null otherwise</returns>
    Task<SupplierDto?> GetSupplierByIdAsync(string supplierId);

    /// <summary>
    /// Gets a supplier by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Supplier DTO if found, null otherwise</returns>
    Task<SupplierDto?> GetSupplierByUserIdAsync(string userId);

    /// <summary>
    /// Gets all suppliers (admin only)
    /// </summary>
    /// <param name="status">Optional verification status filter</param>
    /// <returns>List of supplier DTOs</returns>
    Task<List<SupplierDto>> GetAllSuppliersAsync(SupplierVerificationStatus? status = null);

    /// <summary>
    /// Gets all verified and active suppliers
    /// </summary>
    /// <returns>List of verified supplier DTOs</returns>
    Task<List<SupplierDto>> GetVerifiedSuppliersAsync();

    /// <summary>
    /// Updates a supplier's information
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="request">Update request</param>
    /// <param name="keyManagementService">Key management service for encrypting bank details</param>
    /// <returns>Updated supplier DTO</returns>
    Task<SupplierDto> UpdateSupplierAsync(string supplierId, UpdateSupplierRequest request, IKeyManagementService keyManagementService);

    /// <summary>
    /// Verifies a supplier registration (admin only)
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <returns>Updated supplier DTO</returns>
    Task<SupplierDto> VerifySupplierAsync(string supplierId);

    /// <summary>
    /// Rejects a supplier registration (admin only)
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <returns>Updated supplier DTO</returns>
    Task<SupplierDto> RejectSupplierAsync(string supplierId);

    /// <summary>
    /// Activates a supplier
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <returns>Updated supplier DTO</returns>
    Task<SupplierDto> ActivateSupplierAsync(string supplierId);

    /// <summary>
    /// Deactivates a supplier
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <returns>Updated supplier DTO</returns>
    Task<SupplierDto> DeactivateSupplierAsync(string supplierId);

    /// <summary>
    /// Gets supplier's shipments
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <returns>List of supplier shipment DTOs</returns>
    Task<List<SupplierShipmentDto>> GetSupplierShipmentsAsync(string supplierId);

    /// <summary>
    /// Gets supplier's payment history
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <returns>List of payment DTOs</returns>
    Task<List<PaymentHistoryDto>> GetSupplierPaymentHistoryAsync(string supplierId);
}
