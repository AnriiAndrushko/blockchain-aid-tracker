namespace BlockchainAidTracker.Core.Interfaces;

/// <summary>
/// Service for digital signature operations using ECDSA.
/// </summary>
public interface IDigitalSignatureService
{
    /// <summary>
    /// Generates a new ECDSA key pair.
    /// </summary>
    /// <returns>Tuple containing (PublicKey, PrivateKey) as base64 strings.</returns>
    (string PublicKey, string PrivateKey) GenerateKeyPair();

    /// <summary>
    /// Signs data using a private key.
    /// </summary>
    /// <param name="data">The data to sign.</param>
    /// <param name="privateKey">The private key (base64 encoded).</param>
    /// <returns>Digital signature as base64 string.</returns>
    string SignData(string data, string privateKey);

    /// <summary>
    /// Verifies a digital signature.
    /// </summary>
    /// <param name="data">The original data.</param>
    /// <param name="signature">The signature to verify (base64 encoded).</param>
    /// <param name="publicKey">The public key (base64 encoded).</param>
    /// <returns>True if signature is valid, false otherwise.</returns>
    bool VerifySignature(string data, string signature, string publicKey);
}
