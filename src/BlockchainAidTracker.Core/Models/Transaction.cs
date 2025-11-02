namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Represents a transaction in the blockchain.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Unique identifier for the transaction.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The type of transaction being performed.
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// The timestamp when the transaction was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Public key of the sender/creator of the transaction.
    /// </summary>
    public string SenderPublicKey { get; set; } = string.Empty;

    /// <summary>
    /// The payload data for the transaction (JSON serialized).
    /// </summary>
    public string PayloadData { get; set; } = string.Empty;

    /// <summary>
    /// Digital signature of the transaction for verification.
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new transaction with a generated ID and timestamp.
    /// </summary>
    public Transaction()
    {
        Id = Guid.NewGuid().ToString();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new transaction with specified parameters.
    /// </summary>
    public Transaction(TransactionType type, string senderPublicKey, string payloadData)
    {
        Id = Guid.NewGuid().ToString();
        Timestamp = DateTime.UtcNow;
        Type = type;
        SenderPublicKey = senderPublicKey;
        PayloadData = payloadData;
    }
}
