using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Blockchain.Interfaces;

/// <summary>
/// Interface for blockchain persistence operations.
/// Enables saving and loading blockchain data to/from persistent storage.
/// </summary>
public interface IBlockchainPersistence
{
    /// <summary>
    /// Saves the blockchain chain and pending transactions to persistent storage.
    /// </summary>
    /// <param name="chain">The list of blocks in the blockchain.</param>
    /// <param name="pendingTransactions">The list of pending transactions.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAsync(List<Block> chain, List<Transaction> pendingTransactions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the blockchain chain and pending transactions from persistent storage.
    /// Returns null if no persisted data exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A tuple containing the chain and pending transactions, or null if no data exists.</returns>
    Task<(List<Block> Chain, List<Transaction> PendingTransactions)?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if persisted blockchain data exists.
    /// </summary>
    /// <returns>True if persisted data exists, false otherwise.</returns>
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all persisted blockchain data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(CancellationToken cancellationToken = default);
}
