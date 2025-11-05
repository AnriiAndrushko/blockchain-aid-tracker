using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Blockchain.Consensus;

/// <summary>
/// Interface for blockchain consensus mechanisms.
/// Defines the contract for creating and validating blocks according to consensus rules.
/// </summary>
public interface IConsensusEngine
{
    /// <summary>
    /// Creates a new block from pending transactions using the consensus algorithm.
    /// </summary>
    /// <param name="blockchain">The blockchain instance to create the block for.</param>
    /// <param name="validatorPassword">Password to decrypt the validator's private key for signing.</param>
    /// <returns>A newly created and signed block ready to be added to the chain.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no pending transactions exist, no active validators are available,
    /// or the validator's private key cannot be decrypted.
    /// </exception>
    Task<Block> CreateBlockAsync(Blockchain blockchain, string validatorPassword);

    /// <summary>
    /// Validates whether a block meets the consensus rules.
    /// </summary>
    /// <param name="block">The block to validate.</param>
    /// <param name="previousBlock">The previous block in the chain.</param>
    /// <returns>True if the block is valid according to consensus rules; otherwise, false.</returns>
    bool ValidateBlock(Block block, Block previousBlock);

    /// <summary>
    /// Gets the current block proposer (validator) according to the consensus algorithm.
    /// </summary>
    /// <returns>The validator ID who should propose the next block, or null if no validators are available.</returns>
    Task<string?> GetCurrentBlockProposerAsync();

    /// <summary>
    /// Records that a block was successfully created by a validator.
    /// Updates validator statistics and consensus state.
    /// </summary>
    /// <param name="validatorId">The ID of the validator who created the block.</param>
    Task RecordBlockCreationAsync(string validatorId);
}
