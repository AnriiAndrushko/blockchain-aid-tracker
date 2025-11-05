using BlockchainAidTracker.Services.DTOs.Blockchain;

namespace BlockchainAidTracker.Services.DTOs.Consensus;

/// <summary>
/// Data transfer object for block creation results
/// </summary>
public class BlockCreationResultDto
{
    /// <summary>
    /// Whether the block was created successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result (success or failure reason)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The created block (if successful)
    /// </summary>
    public BlockDto? Block { get; set; }

    /// <summary>
    /// ID of the validator who created the block
    /// </summary>
    public string? ValidatorId { get; set; }

    /// <summary>
    /// Name of the validator who created the block
    /// </summary>
    public string? ValidatorName { get; set; }

    /// <summary>
    /// Number of transactions included in the block
    /// </summary>
    public int TransactionCount { get; set; }
}
