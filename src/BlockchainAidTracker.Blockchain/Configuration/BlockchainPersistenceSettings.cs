namespace BlockchainAidTracker.Blockchain.Configuration;

/// <summary>
/// Configuration settings for blockchain persistence.
/// </summary>
public class BlockchainPersistenceSettings
{
    /// <summary>
    /// Gets or sets whether blockchain persistence is enabled.
    /// Default: false (persistence disabled).
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the file path where blockchain data will be persisted.
    /// Default: "blockchain-data.json".
    /// </summary>
    public string FilePath { get; set; } = "blockchain-data.json";

    /// <summary>
    /// Gets or sets whether to automatically save after each block creation.
    /// Default: true.
    /// </summary>
    public bool AutoSaveAfterBlockCreation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to automatically load blockchain on startup.
    /// Default: true.
    /// </summary>
    public bool AutoLoadOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to create a backup before overwriting existing data.
    /// Default: true.
    /// </summary>
    public bool CreateBackup { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of backup files to keep.
    /// Default: 5. Set to 0 to disable backup rotation.
    /// </summary>
    public int MaxBackupFiles { get; set; } = 5;
}
