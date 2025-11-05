using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.Blockchain;

/// <summary>
/// Data transfer object for blockchain transaction information
/// </summary>
public class TransactionDto
{
    /// <summary>
    /// Unique identifier for the transaction
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The type of transaction being performed
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the transaction was created
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Public key of the sender/creator of the transaction
    /// </summary>
    public string SenderPublicKey { get; set; } = string.Empty;

    /// <summary>
    /// The payload data for the transaction (JSON serialized)
    /// </summary>
    public string PayloadData { get; set; } = string.Empty;

    /// <summary>
    /// Digital signature of the transaction for verification
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Creates a TransactionDto from a Transaction model
    /// </summary>
    public static TransactionDto FromTransaction(Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type.ToString(),
            Timestamp = transaction.Timestamp,
            SenderPublicKey = transaction.SenderPublicKey,
            PayloadData = transaction.PayloadData,
            Signature = transaction.Signature
        };
    }
}
