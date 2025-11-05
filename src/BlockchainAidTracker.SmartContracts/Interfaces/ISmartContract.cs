using BlockchainAidTracker.SmartContracts.Models;

namespace BlockchainAidTracker.SmartContracts.Interfaces;

/// <summary>
/// Interface for smart contracts that can be executed on the blockchain
/// </summary>
public interface ISmartContract
{
    /// <summary>
    /// Unique identifier for this contract
    /// </summary>
    string ContractId { get; }

    /// <summary>
    /// Name of the contract
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what the contract does
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Version of the contract
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Determines if this contract can handle the given transaction
    /// </summary>
    bool CanExecute(ContractExecutionContext context);

    /// <summary>
    /// Executes the contract logic
    /// </summary>
    Task<ContractExecutionResult> ExecuteAsync(ContractExecutionContext context);

    /// <summary>
    /// Gets the current state of the contract
    /// </summary>
    Dictionary<string, object> GetState();

    /// <summary>
    /// Updates the contract state
    /// </summary>
    void UpdateState(Dictionary<string, object> stateChanges);
}
