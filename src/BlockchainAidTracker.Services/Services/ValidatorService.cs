using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.Validator;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;

namespace BlockchainAidTracker.Services.Services;

/// <summary>
/// Implementation of validator management service
/// </summary>
public class ValidatorService : IValidatorService
{
    private readonly IValidatorRepository _validatorRepository;
    private readonly IDigitalSignatureService _digitalSignatureService;
    private readonly IKeyManagementService _keyManagementService;

    public ValidatorService(
        IValidatorRepository validatorRepository,
        IDigitalSignatureService digitalSignatureService,
        IKeyManagementService keyManagementService)
    {
        _validatorRepository = validatorRepository ?? throw new ArgumentNullException(nameof(validatorRepository));
        _digitalSignatureService = digitalSignatureService ?? throw new ArgumentNullException(nameof(digitalSignatureService));
        _keyManagementService = keyManagementService ?? throw new ArgumentNullException(nameof(keyManagementService));
    }

    public async Task<ValidatorDto> RegisterValidatorAsync(CreateValidatorRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Validate name uniqueness
        if (await _validatorRepository.NameExistsAsync(request.Name))
        {
            throw new BusinessException($"Validator name '{request.Name}' is already taken");
        }

        // Generate ECDSA key pair for the validator
        var (publicKey, privateKey) = _digitalSignatureService.GenerateKeyPair();

        // Encrypt private key with validator password
        var encryptedPrivateKey = _keyManagementService.EncryptPrivateKey(privateKey, request.Password);

        // Create validator entity
        var validator = new Validator(
            name: request.Name,
            publicKey: publicKey,
            encryptedPrivateKey: encryptedPrivateKey,
            priority: request.Priority,
            address: request.Address,
            description: request.Description
        );

        // Save to database
        await _validatorRepository.AddAsync(validator);
        await _validatorRepository.SaveChangesAsync();

        return MapToDto(validator);
    }

    public async Task<ValidatorDto?> GetValidatorByIdAsync(string validatorId)
    {
        if (string.IsNullOrWhiteSpace(validatorId))
        {
            throw new ArgumentException("Validator ID cannot be null or empty", nameof(validatorId));
        }

        var validator = await _validatorRepository.GetByIdAsync(validatorId);
        return validator != null ? MapToDto(validator) : null;
    }

    public async Task<ValidatorDto?> GetValidatorByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Validator name cannot be null or empty", nameof(name));
        }

        var validator = await _validatorRepository.GetByNameAsync(name);
        return validator != null ? MapToDto(validator) : null;
    }

    public async Task<ValidatorDto?> GetValidatorByPublicKeyAsync(string publicKey)
    {
        if (string.IsNullOrWhiteSpace(publicKey))
        {
            throw new ArgumentException("Public key cannot be null or empty", nameof(publicKey));
        }

        var validator = await _validatorRepository.GetByPublicKeyAsync(publicKey);
        return validator != null ? MapToDto(validator) : null;
    }

    public async Task<List<ValidatorDto>> GetAllValidatorsAsync(bool activeOnly = false)
    {
        var validators = activeOnly
            ? await _validatorRepository.GetActiveValidatorsAsync()
            : await _validatorRepository.GetAllValidatorsOrderedByPriorityAsync();

        return validators.Select(MapToDto).ToList();
    }

    public async Task<ValidatorDto?> GetNextValidatorForBlockCreationAsync()
    {
        var validator = await _validatorRepository.GetNextValidatorForBlockCreationAsync();
        return validator != null ? MapToDto(validator) : null;
    }

    public async Task<ValidatorDto> UpdateValidatorAsync(string validatorId, UpdateValidatorRequest request)
    {
        if (string.IsNullOrWhiteSpace(validatorId))
        {
            throw new ArgumentException("Validator ID cannot be null or empty", nameof(validatorId));
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var validator = await _validatorRepository.GetByIdAsync(validatorId);
        if (validator == null)
        {
            throw new NotFoundException($"Validator with ID '{validatorId}' not found");
        }

        // Update fields if provided
        if (request.Priority.HasValue)
        {
            validator.UpdatePriority(request.Priority.Value);
        }

        if (request.Address != null)
        {
            validator.UpdateAddress(request.Address);
        }

        if (request.Description != null)
        {
            validator.Description = request.Description;
            validator.UpdatedTimestamp = DateTime.UtcNow;
        }

        _validatorRepository.Update(validator);
        await _validatorRepository.SaveChangesAsync();

        return MapToDto(validator);
    }

    public async Task<bool> ActivateValidatorAsync(string validatorId)
    {
        if (string.IsNullOrWhiteSpace(validatorId))
        {
            throw new ArgumentException("Validator ID cannot be null or empty", nameof(validatorId));
        }

        var validator = await _validatorRepository.GetByIdAsync(validatorId);
        if (validator == null)
        {
            throw new NotFoundException($"Validator with ID '{validatorId}' not found");
        }

        validator.Activate();
        _validatorRepository.Update(validator);
        await _validatorRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivateValidatorAsync(string validatorId)
    {
        if (string.IsNullOrWhiteSpace(validatorId))
        {
            throw new ArgumentException("Validator ID cannot be null or empty", nameof(validatorId));
        }

        var validator = await _validatorRepository.GetByIdAsync(validatorId);
        if (validator == null)
        {
            throw new NotFoundException($"Validator with ID '{validatorId}' not found");
        }

        validator.Deactivate();
        _validatorRepository.Update(validator);
        await _validatorRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RecordBlockCreationAsync(string validatorId)
    {
        if (string.IsNullOrWhiteSpace(validatorId))
        {
            throw new ArgumentException("Validator ID cannot be null or empty", nameof(validatorId));
        }

        var validator = await _validatorRepository.GetByIdAsync(validatorId);
        if (validator == null)
        {
            throw new NotFoundException($"Validator with ID '{validatorId}' not found");
        }

        validator.RecordBlockCreation();
        _validatorRepository.Update(validator);
        await _validatorRepository.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetActiveValidatorCountAsync()
    {
        return await _validatorRepository.GetActiveValidatorCountAsync();
    }

    /// <summary>
    /// Maps a Validator entity to a ValidatorDto
    /// </summary>
    private static ValidatorDto MapToDto(Validator validator)
    {
        return new ValidatorDto
        {
            Id = validator.Id,
            Name = validator.Name,
            PublicKey = validator.PublicKey,
            Address = validator.Address,
            IsActive = validator.IsActive,
            Priority = validator.Priority,
            CreatedTimestamp = validator.CreatedTimestamp,
            UpdatedTimestamp = validator.UpdatedTimestamp,
            LastBlockCreatedTimestamp = validator.LastBlockCreatedTimestamp,
            TotalBlocksCreated = validator.TotalBlocksCreated,
            Description = validator.Description
        };
    }
}
