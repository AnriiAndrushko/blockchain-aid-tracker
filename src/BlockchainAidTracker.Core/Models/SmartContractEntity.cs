namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Entity for persisting smart contract deployment and state information in the database
/// </summary>
public class SmartContractEntity
{
    /// <summary>
    /// Unique identifier for the contract (same as contract ID)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name of the contract
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the contract does
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Version of the contract
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Contract type (class name)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// When the contract was deployed
    /// </summary>
    public DateTime DeployedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the contract is currently enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Serialized JSON state of the contract
    /// </summary>
    public string StateJson { get; set; } = "{}";

    /// <summary>
    /// When the state was last updated
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
