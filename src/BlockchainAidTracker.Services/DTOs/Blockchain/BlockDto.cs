using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.Blockchain;

/// <summary>
/// Data transfer object for blockchain block information
/// </summary>
public class BlockDto
{
    /// <summary>
    /// The position of this block in the chain
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The timestamp when this block was created
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// List of transactions included in this block
    /// </summary>
    public List<TransactionDto> Transactions { get; set; } = new();

    /// <summary>
    /// Hash of the previous block in the chain
    /// </summary>
    public string PreviousHash { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the current block
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Nonce value used for proof-of-work
    /// </summary>
    public long Nonce { get; set; }

    /// <summary>
    /// Public key of the validator who created/validated this block
    /// </summary>
    public string ValidatorPublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Digital signature from the validator for this block
    /// </summary>
    public string ValidatorSignature { get; set; } = string.Empty;

    /// <summary>
    /// Creates a BlockDto from a Block model
    /// </summary>
    public static BlockDto FromBlock(Block block)
    {
        return new BlockDto
        {
            Index = block.Index,
            Timestamp = block.Timestamp,
            Transactions = block.Transactions.Select(TransactionDto.FromTransaction).ToList(),
            PreviousHash = block.PreviousHash,
            Hash = block.Hash,
            Nonce = block.Nonce,
            ValidatorPublicKey = block.ValidatorPublicKey,
            ValidatorSignature = block.ValidatorSignature
        };
    }
}
