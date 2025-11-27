namespace BlockchainAidTracker.Services.Configuration;

/// <summary>
/// Configuration settings for consensus mechanism and automated block creation
/// </summary>
public class ConsensusSettings
{
    /// <summary>
    /// Interval in seconds between automated block creation attempts.
    /// Default: 30 seconds
    /// </summary>
    public int BlockCreationIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Minimum number of pending transactions required before creating a new block.
    /// Default: 1 (create block with any pending transactions)
    /// </summary>
    public int MinimumTransactionsPerBlock { get; set; } = 1;

    /// <summary>
    /// Maximum number of pending transactions to include in a single block.
    /// Default: 100
    /// </summary>
    public int MaximumTransactionsPerBlock { get; set; } = 100;

    /// <summary>
    /// Password for validator private key decryption.
    /// In production, this should be retrieved from secure storage (Azure Key Vault, AWS KMS, etc.)
    /// Default: "ValidatorPassword123!" (for prototype/demo only)
    /// </summary>
    public string ValidatorPassword { get; set; } = "ValidatorPassword123!";

    /// <summary>
    /// Enable or disable automated block creation background service.
    /// Default: true
    /// </summary>
    public bool EnableAutomatedBlockCreation { get; set; } = true;

    /// <summary>
    /// Validator selection strategy for block creation.
    /// Options: "RoundRobin" (deterministic), "Random" (production-like)
    /// Default: "RoundRobin"
    /// </summary>
    public string ValidatorSelectionStrategy { get; set; } = "RoundRobin";
}
