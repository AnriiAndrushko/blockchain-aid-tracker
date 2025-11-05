using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.SmartContracts.Models;

/// <summary>
/// Provides context information for smart contract execution
/// </summary>
public class ContractExecutionContext
{
    /// <summary>
    /// The transaction that triggered the contract execution
    /// </summary>
    public Transaction Transaction { get; init; }

    /// <summary>
    /// The current block being processed (if applicable)
    /// </summary>
    public Block? CurrentBlock { get; init; }

    /// <summary>
    /// Timestamp of the execution
    /// </summary>
    public DateTime ExecutionTime { get; init; }

    /// <summary>
    /// Additional data that can be passed to the contract
    /// </summary>
    public Dictionary<string, object> Data { get; init; }

    public ContractExecutionContext(Transaction transaction)
    {
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        ExecutionTime = DateTime.UtcNow;
        Data = new Dictionary<string, object>();
    }

    public ContractExecutionContext(Transaction transaction, Block currentBlock, Dictionary<string, object>? data = null)
    {
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        CurrentBlock = currentBlock;
        ExecutionTime = DateTime.UtcNow;
        Data = data ?? new Dictionary<string, object>();
    }
}
