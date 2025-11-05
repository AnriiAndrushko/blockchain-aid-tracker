namespace BlockchainAidTracker.Services.DTOs.Blockchain;

/// <summary>
/// Data transfer object for blockchain validation result
/// </summary>
public class ValidationResultDto
{
    /// <summary>
    /// Indicates whether the blockchain is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The number of blocks in the chain
    /// </summary>
    public int BlockCount { get; set; }

    /// <summary>
    /// Optional error messages if validation failed
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Timestamp when validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; set; }
}
