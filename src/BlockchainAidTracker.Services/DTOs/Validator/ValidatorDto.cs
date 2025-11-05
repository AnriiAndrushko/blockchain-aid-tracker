namespace BlockchainAidTracker.Services.DTOs.Validator;

/// <summary>
/// DTO for validator data
/// </summary>
public class ValidatorDto
{
    /// <summary>
    /// Validator ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Validator name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Public key
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Network address
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Whether the validator is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Priority in validator set
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedTimestamp { get; set; }

    /// <summary>
    /// Last block creation timestamp
    /// </summary>
    public DateTime? LastBlockCreatedTimestamp { get; set; }

    /// <summary>
    /// Total blocks created by this validator
    /// </summary>
    public int TotalBlocksCreated { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }
}
