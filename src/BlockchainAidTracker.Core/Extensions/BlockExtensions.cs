using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Core.Extensions;

/// <summary>
/// Extension methods for Block operations.
/// </summary>
public static class BlockExtensions
{
    /// <summary>
    /// Generates the data string to be signed by the validator for a block.
    /// </summary>
    public static string GetValidatorSignatureData(this Block block)
    {
        // Sign the block hash and index to prove the validator approved this specific block
        return $"{block.Index}{block.Hash}{block.Timestamp:O}{block.ValidatorPublicKey}";
    }

    /// <summary>
    /// Signs the block with the validator's private key.
    /// </summary>
    public static void SignBlock(this Block block, string validatorPrivateKey, IDigitalSignatureService signatureService)
    {
        if (string.IsNullOrEmpty(validatorPrivateKey))
        {
            throw new ArgumentException("Validator private key cannot be null or empty.", nameof(validatorPrivateKey));
        }

        if (signatureService == null)
        {
            throw new ArgumentNullException(nameof(signatureService));
        }

        if (string.IsNullOrEmpty(block.Hash))
        {
            throw new InvalidOperationException("Block must have a hash before it can be signed.");
        }

        var dataToSign = block.GetValidatorSignatureData();
        block.ValidatorSignature = signatureService.SignData(dataToSign, validatorPrivateKey);
    }

    /// <summary>
    /// Verifies the validator's signature on the block.
    /// </summary>
    public static bool VerifyValidatorSignature(this Block block, IDigitalSignatureService signatureService)
    {
        if (signatureService == null)
        {
            throw new ArgumentNullException(nameof(signatureService));
        }

        if (string.IsNullOrEmpty(block.ValidatorSignature))
        {
            return false;
        }

        if (string.IsNullOrEmpty(block.ValidatorPublicKey))
        {
            return false;
        }

        // Genesis block doesn't require signature validation
        if (block.Index == 0 && block.ValidatorPublicKey == "GENESIS")
        {
            return true;
        }

        var dataToSign = block.GetValidatorSignatureData();
        return signatureService.VerifySignature(dataToSign, block.ValidatorSignature, block.ValidatorPublicKey);
    }
}
