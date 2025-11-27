using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository interface for Supplier-specific operations
/// </summary>
public interface ISupplierRepository : IRepository<Supplier>
{
    /// <summary>
    /// Gets a supplier by company name
    /// </summary>
    /// <param name="companyName">Company name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Supplier if found, null otherwise</returns>
    Task<Supplier?> GetByCompanyNameAsync(string companyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all suppliers with a specific verification status
    /// </summary>
    /// <param name="status">Verification status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of suppliers with the specified status</returns>
    Task<List<Supplier>> GetByVerificationStatusAsync(SupplierVerificationStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active suppliers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active suppliers</returns>
    Task<List<Supplier>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all verified suppliers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of verified and active suppliers</returns>
    Task<List<Supplier>> GetVerifiedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a supplier by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Supplier if found, null otherwise</returns>
    Task<Supplier?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supplier's shipments with related data
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of supplier shipments</returns>
    Task<List<SupplierShipment>> GetSupplierShipmentsAsync(string supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a company name is already registered
    /// </summary>
    /// <param name="companyName">Company name to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if company name exists, false otherwise</returns>
    Task<bool> CompanyNameExistsAsync(string companyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tax ID is already registered
    /// </summary>
    /// <param name="taxId">Tax ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if tax ID exists, false otherwise</returns>
    Task<bool> TaxIdExistsAsync(string taxId, CancellationToken cancellationToken = default);
}
