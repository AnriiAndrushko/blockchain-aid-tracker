namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Represents a block in the blockchain.
/// </summary>
public class Block
{
    /// <summary>
    /// The position of this block in the chain.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The timestamp when this block was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// List of transactions included in this block.
    /// </summary>
    public List<Transaction> Transactions { get; set; } = new();

    /// <summary>
    /// Hash of the previous block in the chain.
    /// </summary>
    public string PreviousHash { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the current block.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Nonce value used for proof-of-work (optional for PoA, but included for future use).
    /// </summary>
    public long Nonce { get; set; }

    /// <summary>
    /// Public key of the validator who created/validated this block.
    /// </summary>
    public string ValidatorPublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Digital signature from the validator for this block.
    /// </summary>
    public string ValidatorSignature { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new block with default values.
    /// </summary>
    public Block()
    {
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new block with specified parameters.
    /// </summary>
    public Block(int index, List<Transaction> transactions, string previousHash)
    {
        Index = index;
        Timestamp = DateTime.UtcNow;
        Transactions = transactions;
        PreviousHash = previousHash;
        Nonce = 0;
    }

    /// <summary>
    /// Calculates the data string used for hashing this block.
    /// </summary>
    public string CalculateHashData()
    {
        var transactionData = string.Join(",", Transactions.Select(t => t.Id));
        return $"{Index}{Timestamp:O}{transactionData}{PreviousHash}{Nonce}{ValidatorPublicKey}";
    }
}
