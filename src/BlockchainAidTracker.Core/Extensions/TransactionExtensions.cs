using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Core.Extensions;

/// <summary>
/// Extension methods for Transaction operations.
/// </summary>
public static class TransactionExtensions
{
    /// <summary>
    /// Generates the data string to be signed for a transaction.
    /// </summary>
    public static string GetSignatureData(this Transaction transaction)
    {
        return $"{transaction.Id}{transaction.Type}{transaction.Timestamp:O}{transaction.SenderPublicKey}{transaction.PayloadData}";
    }

    /// <summary>
    /// Signs the transaction with the sender's private key.
    /// </summary>
    public static void Sign(this Transaction transaction, string privateKey, IDigitalSignatureService signatureService)
    {
        if (string.IsNullOrEmpty(privateKey))
        {
            throw new ArgumentException("Private key cannot be null or empty.", nameof(privateKey));
        }

        if (signatureService == null)
        {
            throw new ArgumentNullException(nameof(signatureService));
        }

        var dataToSign = transaction.GetSignatureData();
        transaction.Signature = signatureService.SignData(dataToSign, privateKey);
    }

    /// <summary>
    /// Verifies the transaction signature.
    /// </summary>
    public static bool VerifySignature(this Transaction transaction, IDigitalSignatureService signatureService)
    {
        if (signatureService == null)
        {
            throw new ArgumentNullException(nameof(signatureService));
        }

        if (string.IsNullOrEmpty(transaction.Signature))
        {
            return false;
        }

        if (string.IsNullOrEmpty(transaction.SenderPublicKey))
        {
            return false;
        }

        var dataToSign = transaction.GetSignatureData();
        return signatureService.VerifySignature(dataToSign, transaction.Signature, transaction.SenderPublicKey);
    }
}
