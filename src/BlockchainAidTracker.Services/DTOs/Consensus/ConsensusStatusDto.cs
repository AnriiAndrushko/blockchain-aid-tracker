namespace BlockchainAidTracker.Services.DTOs.Consensus;

/// <summary>
/// Data transfer object for consensus status information
/// </summary>
public class ConsensusStatusDto
{
    /// <summary>
    /// Current blockchain height (number of blocks)
    /// </summary>
    public int ChainHeight { get; set; }

    /// <summary>
    /// Number of pending transactions waiting to be included in a block
    /// </summary>
    public int PendingTransactionCount { get; set; }

    /// <summary>
    /// ID of the next validator who will create a block
    /// </summary>
    public string? NextValidatorId { get; set; }

    /// <summary>
    /// Name of the next validator who will create a block
    /// </summary>
    public string? NextValidatorName { get; set; }

    /// <summary>
    /// Total number of active validators in the network
    /// </summary>
    public int ActiveValidatorCount { get; set; }

    /// <summary>
    /// Timestamp of the last block created
    /// </summary>
    public DateTime? LastBlockTimestamp { get; set; }

    /// <summary>
    /// Hash of the last block in the chain
    /// </summary>
    public string? LastBlockHash { get; set; }

    /// <summary>
    /// Whether automated block creation is enabled
    /// </summary>
    public bool AutomatedBlockCreationEnabled { get; set; }

    /// <summary>
    /// Block creation interval in seconds
    /// </summary>
    public int BlockCreationIntervalSeconds { get; set; }
}
