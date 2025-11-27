using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Validator selection strategy enumeration
/// </summary>
public enum ValidatorSelectionStrategyType
{
    /// <summary>
    /// Select validators in round-robin order by priority
    /// </summary>
    RoundRobin = 0,

    /// <summary>
    /// Select validators randomly from active validators
    /// </summary>
    Random = 1
}

/// <summary>
/// Repository implementation for Validator-specific operations
/// </summary>
public class ValidatorRepository : Repository<Validator>, IValidatorRepository
{
    private int _roundRobinIndex = 0;
    private ValidatorSelectionStrategyType _selectionStrategy = ValidatorSelectionStrategyType.RoundRobin;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public ValidatorRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Sets the validator selection strategy
    /// </summary>
    public void SetSelectionStrategy(ValidatorSelectionStrategyType strategy)
    {
        _selectionStrategy = strategy;
    }

    /// <inheritdoc />
    public async Task<Validator?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(v => v.Name == name, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Validator?> GetByPublicKeyAsync(string publicKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(v => v.PublicKey == publicKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Validator>> GetActiveValidatorsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.IsActive)
            .OrderBy(v => v.Priority)
            .ThenBy(v => v.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Validator>> GetAllValidatorsOrderedByPriorityAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderBy(v => v.Priority)
            .ThenBy(v => v.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(v => v.Name == name, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> PublicKeyExistsAsync(string publicKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(v => v.PublicKey == publicKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetActiveValidatorCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(v => v.IsActive, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Validator?> GetNextValidatorForBlockCreationAsync(CancellationToken cancellationToken = default)
    {
        // Get all active validators
        var activeValidators = await GetActiveValidatorsAsync(cancellationToken);

        if (activeValidators.Count == 0)
        {
            return null;
        }

        // If only one validator, return it
        if (activeValidators.Count == 1)
        {
            return activeValidators[0];
        }

        // Select based on configured strategy
        return _selectionStrategy switch
        {
            ValidatorSelectionStrategyType.RoundRobin => SelectValidatorRoundRobin(activeValidators),
            ValidatorSelectionStrategyType.Random => SelectValidatorRandom(activeValidators),
            _ => activeValidators[0]
        };
    }

    /// <summary>
    /// Select validator using round-robin strategy (fair distribution)
    /// </summary>
    private Validator SelectValidatorRoundRobin(List<Validator> activeValidators)
    {
        var validator = activeValidators[_roundRobinIndex];
        _roundRobinIndex = (_roundRobinIndex + 1) % activeValidators.Count;
        return validator;
    }

    /// <summary>
    /// Select validator using random strategy
    /// </summary>
    private Validator SelectValidatorRandom(List<Validator> activeValidators)
    {
        var random = new Random();
        var randomIndex = random.Next(activeValidators.Count);
        return activeValidators[randomIndex];
    }
}
