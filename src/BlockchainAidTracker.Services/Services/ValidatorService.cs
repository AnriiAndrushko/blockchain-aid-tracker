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
    private readonly IAuditLogService _auditLogService;

    public ValidatorService(
        IValidatorRepository validatorRepository,
        IDigitalSignatureService digitalSignatureService,
        IKeyManagementService keyManagementService,
        IAuditLogService auditLogService)
    {
        _validatorRepository = validatorRepository ?? throw new ArgumentNullException(nameof(validatorRepository));
        _digitalSignatureService = digitalSignatureService ?? throw new ArgumentNullException(nameof(digitalSignatureService));
        _keyManagementService = keyManagementService ?? throw new ArgumentNullException(nameof(keyManagementService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    public async Task<ValidatorDto> RegisterValidatorAsync(CreateValidatorRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        try
        {
            // Validate name uniqueness
            if (await _validatorRepository.NameExistsAsync(request.Name))
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Validator,
                    AuditLogAction.ValidatorRegistered,
                    $"Validator registration failed: Name '{request.Name}' already exists",
                    "Validator name already taken");
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

            // Save to database (AddAsync handles SaveChanges internally)
            await _validatorRepository.AddAsync(validator);

            // Log successful validator registration
            await _auditLogService.LogAsync(
                AuditLogCategory.Validator,
                AuditLogAction.ValidatorRegistered,
                $"Validator '{request.Name}' registered successfully with priority {request.Priority}",
                entityId: validator.Id,
                entityType: "Validator",
                metadata: $"{{\"priority\":{request.Priority},\"address\":\"{request.Address}\"}}");

            return MapToDto(validator);
        }
        catch (BusinessException)
        {
            throw; // Re-throw business exceptions (already logged)
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                AuditLogCategory.Validator,
                AuditLogAction.ValidatorRegistered,
                $"Validator registration failed for name '{request.Name}'",
                ex.Message);
            throw;
        }
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

        try
        {
            var validator = await _validatorRepository.GetByIdAsync(validatorId);
            if (validator == null)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Validator,
                    AuditLogAction.ValidatorUpdated,
                    $"Validator update failed: Validator '{validatorId}' not found",
                    "Validator not found",
                    entityId: validatorId,
                    entityType: "Validator");
                throw new NotFoundException($"Validator with ID '{validatorId}' not found");
            }

            var changes = new List<string>();

            // Update fields if provided
            if (request.Priority.HasValue)
            {
                var oldPriority = validator.Priority;
                validator.UpdatePriority(request.Priority.Value);
                changes.Add($"priority: {oldPriority} -> {request.Priority.Value}");
            }

            if (request.Address != null)
            {
                var oldAddress = validator.Address;
                validator.UpdateAddress(request.Address);
                changes.Add($"address: '{oldAddress}' -> '{request.Address}'");
            }

            if (request.Description != null)
            {
                validator.Description = request.Description;
                validator.UpdatedTimestamp = DateTime.UtcNow;
                changes.Add($"description updated");
            }

            // Update handles SaveChanges internally
            _validatorRepository.Update(validator);

            // Log successful update
            await _auditLogService.LogAsync(
                AuditLogCategory.Validator,
                AuditLogAction.ValidatorUpdated,
                $"Validator '{validator.Name}' updated: {string.Join(", ", changes)}",
                entityId: validatorId,
                entityType: "Validator",
                metadata: $"{{\"changes\":[{string.Join(",", changes.Select(c => $"\"{c}\""))}]}}");

            return MapToDto(validator);
        }
        catch (NotFoundException)
        {
            throw; // Re-throw not found exceptions (already logged)
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                AuditLogCategory.Validator,
                AuditLogAction.ValidatorUpdated,
                $"Validator update failed for validator '{validatorId}'",
                ex.Message,
                entityId: validatorId,
                entityType: "Validator");
            throw;
        }
    }

    public async Task<bool> ActivateValidatorAsync(string validatorId)
    {
        if (string.IsNullOrWhiteSpace(validatorId))
        {
            throw new ArgumentException("Validator ID cannot be null or empty", nameof(validatorId));
        }

        try
        {
            var validator = await _validatorRepository.GetByIdAsync(validatorId);
            if (validator == null)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Validator,
                    AuditLogAction.ValidatorActivated,
                    $"Validator activation failed: Validator '{validatorId}' not found",
                    "Validator not found",
                    entityId: validatorId,
                    entityType: "Validator");
                throw new NotFoundException($"Validator with ID '{validatorId}' not found");
            }

            validator.Activate();
            // Update handles SaveChanges internally
            _validatorRepository.Update(validator);

            // Log successful activation
            await _auditLogService.LogAsync(
                AuditLogCategory.Validator,
                AuditLogAction.ValidatorActivated,
                $"Validator '{validator.Name}' activated",
                entityId: validatorId,
                entityType: "Validator");

            return true;
        }
        catch (NotFoundException)
        {
            throw; // Re-throw not found exceptions (already logged)
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                AuditLogCategory.Validator,
                AuditLogAction.ValidatorActivated,
                $"Validator activation failed for validator '{validatorId}'",
                ex.Message,
                entityId: validatorId,
                entityType: "Validator");
            throw;
        }
    }

    public async Task<bool> DeactivateValidatorAsync(string validatorId)
    {
        if (string.IsNullOrWhiteSpace(validatorId))
        {
            throw new ArgumentException("Validator ID cannot be null or empty", nameof(validatorId));
        }

        try
        {
            var validator = await _validatorRepository.GetByIdAsync(validatorId);
            if (validator == null)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Validator,
                    AuditLogAction.ValidatorDeactivated,
                    $"Validator deactivation failed: Validator '{validatorId}' not found",
                    "Validator not found",
                    entityId: validatorId,
                    entityType: "Validator");
                throw new NotFoundException($"Validator with ID '{validatorId}' not found");
            }

            validator.Deactivate();
            // Update handles SaveChanges internally
            _validatorRepository.Update(validator);

            // Log successful deactivation
            await _auditLogService.LogAsync(
                AuditLogCategory.Validator,
                AuditLogAction.ValidatorDeactivated,
                $"Validator '{validator.Name}' deactivated",
                entityId: validatorId,
                entityType: "Validator");

            return true;
        }
        catch (NotFoundException)
        {
            throw; // Re-throw not found exceptions (already logged)
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                AuditLogCategory.Validator,
                AuditLogAction.ValidatorDeactivated,
                $"Validator deactivation failed for validator '{validatorId}'",
                ex.Message,
                entityId: validatorId,
                entityType: "Validator");
            throw;
        }
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
        // Update handles SaveChanges internally
        _validatorRepository.Update(validator);

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
