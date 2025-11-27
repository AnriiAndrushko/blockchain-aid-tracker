using BlockchainAidTracker.Services.DTOs.Validator;

namespace BlockchainAidTracker.Services.Interfaces;

/// <summary>
/// Service for validator management operations
/// </summary>
public interface IValidatorService
{
    /// <summary>
    /// Registers a new validator in the system
    /// </summary>
    /// <param name="request">Validator registration request</param>
    /// <returns>Newly created validator DTO</returns>
    Task<ValidatorDto> RegisterValidatorAsync(CreateValidatorRequest request);

    /// <summary>
    /// Gets a validator by ID
    /// </summary>
    /// <param name="validatorId">Validator ID</param>
    /// <returns>Validator DTO or null if not found</returns>
    Task<ValidatorDto?> GetValidatorByIdAsync(string validatorId);

    /// <summary>
    /// Gets a validator by name
    /// </summary>
    /// <param name="name">Validator name</param>
    /// <returns>Validator DTO or null if not found</returns>
    Task<ValidatorDto?> GetValidatorByNameAsync(string name);

    /// <summary>
    /// Gets a validator by public key
    /// </summary>
    /// <param name="publicKey">Public key</param>
    /// <returns>Validator DTO or null if not found</returns>
    Task<ValidatorDto?> GetValidatorByPublicKeyAsync(string publicKey);

    /// <summary>
    /// Gets all validators (active and inactive)
    /// </summary>
    /// <param name="activeOnly">If true, returns only active validators</param>
    /// <returns>List of validator DTOs ordered by priority</returns>
    Task<List<ValidatorDto>> GetAllValidatorsAsync(bool activeOnly = false);

    /// <summary>
    /// Gets a random validator for block creation
    /// </summary>
    /// <returns>Validator DTO or null if no active validators exist</returns>
    Task<ValidatorDto?> GetNextValidatorForBlockCreationAsync();

    /// <summary>
    /// Updates validator details
    /// </summary>
    /// <param name="validatorId">Validator ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated validator DTO</returns>
    Task<ValidatorDto> UpdateValidatorAsync(string validatorId, UpdateValidatorRequest request);

    /// <summary>
    /// Activates a validator
    /// </summary>
    /// <param name="validatorId">Validator ID</param>
    /// <returns>True if activated successfully</returns>
    Task<bool> ActivateValidatorAsync(string validatorId);

    /// <summary>
    /// Deactivates a validator
    /// </summary>
    /// <param name="validatorId">Validator ID</param>
    /// <returns>True if deactivated successfully</returns>
    Task<bool> DeactivateValidatorAsync(string validatorId);

    /// <summary>
    /// Records that a validator created a new block (updates statistics)
    /// </summary>
    /// <param name="validatorId">Validator ID</param>
    /// <returns>True if recorded successfully</returns>
    Task<bool> RecordBlockCreationAsync(string validatorId);

    /// <summary>
    /// Gets the count of active validators
    /// </summary>
    /// <returns>Number of active validators</returns>
    Task<int> GetActiveValidatorCountAsync();
}
