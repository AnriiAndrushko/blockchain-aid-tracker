using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository interface for Validator-specific operations
/// </summary>
public interface IValidatorRepository : IRepository<Validator>
{
    /// <summary>
    /// Gets a validator by name
    /// </summary>
    /// <param name="name">Validator name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validator if found, null otherwise</returns>
    Task<Validator?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a validator by public key
    /// </summary>
    /// <param name="publicKey">Public key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validator if found, null otherwise</returns>
    Task<Validator?> GetByPublicKeyAsync(string publicKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active validators ordered by priority
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active validators ordered by priority</returns>
    Task<List<Validator>> GetActiveValidatorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all validators (active and inactive) ordered by priority
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all validators ordered by priority</returns>
    Task<List<Validator>> GetAllValidatorsOrderedByPriorityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a validator name is already taken
    /// </summary>
    /// <param name="name">Validator name to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if name exists, false otherwise</returns>
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a public key is already registered
    /// </summary>
    /// <param name="publicKey">Public key to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if public key exists, false otherwise</returns>
    Task<bool> PublicKeyExistsAsync(string publicKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active validators
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of active validators</returns>
    Task<int> GetActiveValidatorCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a random validator for block creation from active validators
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A randomly selected active validator, or null if no active validators exist</returns>
    Task<Validator?> GetNextValidatorForBlockCreationAsync(CancellationToken cancellationToken = default);
}
