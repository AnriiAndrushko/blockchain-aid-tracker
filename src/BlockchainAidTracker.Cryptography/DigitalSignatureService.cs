using System.Security.Cryptography;
using System.Text;
using BlockchainAidTracker.Core.Interfaces;

namespace BlockchainAidTracker.Cryptography;

/// <summary>
/// Implementation of digital signature service using ECDSA (Elliptic Curve Digital Signature Algorithm).
/// Uses the P-256 curve (secp256r1).
/// </summary>
public class DigitalSignatureService : IDigitalSignatureService
{
    /// <summary>
    /// Generates a new ECDSA key pair using the P-256 curve.
    /// </summary>
    /// <returns>Tuple containing (PublicKey, PrivateKey) as base64 strings.</returns>
    public (string PublicKey, string PrivateKey) GenerateKeyPair()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Export keys
        var privateKeyBytes = ecdsa.ExportECPrivateKey();
        var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();

        var publicKey = Convert.ToBase64String(publicKeyBytes);
        var privateKey = Convert.ToBase64String(privateKeyBytes);

        return (publicKey, privateKey);
    }

    /// <summary>
    /// Signs data using a private key with ECDSA.
    /// </summary>
    /// <param name="data">The data to sign.</param>
    /// <param name="privateKey">The private key (base64 encoded).</param>
    /// <returns>Digital signature as base64 string.</returns>
    public string SignData(string data, string privateKey)
    {
        if (string.IsNullOrEmpty(data))
        {
            throw new ArgumentException("Data cannot be null or empty.", nameof(data));
        }

        if (string.IsNullOrEmpty(privateKey))
        {
            throw new ArgumentException("Private key cannot be null or empty.", nameof(privateKey));
        }

        try
        {
            using var ecdsa = ECDsa.Create();
            var privateKeyBytes = Convert.FromBase64String(privateKey);
            ecdsa.ImportECPrivateKey(privateKeyBytes, out _);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = ecdsa.SignData(dataBytes, HashAlgorithmName.SHA256);

            return Convert.ToBase64String(signatureBytes);
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Failed to sign data.", ex);
        }
    }

    /// <summary>
    /// Verifies a digital signature using ECDSA.
    /// </summary>
    /// <param name="data">The original data.</param>
    /// <param name="signature">The signature to verify (base64 encoded).</param>
    /// <param name="publicKey">The public key (base64 encoded).</param>
    /// <returns>True if signature is valid, false otherwise.</returns>
    public bool VerifySignature(string data, string signature, string publicKey)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(publicKey))
        {
            return false;
        }

        try
        {
            using var ecdsa = ECDsa.Create();
            var publicKeyBytes = Convert.FromBase64String(publicKey);
            ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);

            return ecdsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256);
        }
        catch
        {
            // If any error occurs during verification, the signature is invalid
            return false;
        }
    }
}
