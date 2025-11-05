using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BlockchainAidTracker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for Validator-specific operations
/// </summary>
public class ValidatorRepository : Repository<Validator>, IValidatorRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public ValidatorRepository(ApplicationDbContext context) : base(context)
    {
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
        // Get all active validators ordered by priority
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

        // Find the validator that hasn't created a block in the longest time
        // If none have created blocks, use the first one by priority
        var validator = activeValidators
            .OrderBy(v => v.LastBlockCreatedTimestamp ?? DateTime.MinValue)
            .ThenBy(v => v.Priority)
            .First();

        return validator;
    }
}
