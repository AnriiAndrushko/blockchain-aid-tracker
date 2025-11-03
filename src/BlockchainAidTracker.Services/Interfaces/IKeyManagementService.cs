namespace BlockchainAidTracker.Services.Interfaces;

/// <summary>
/// Service for managing cryptographic key encryption and decryption
/// </summary>
public interface IKeyManagementService
{
    /// <summary>
    /// Encrypts a private key using a password
    /// </summary>
    /// <param name="privateKey">The private key to encrypt</param>
    /// <param name="password">The password to use for encryption</param>
    /// <returns>Encrypted private key as base64 string</returns>
    string EncryptPrivateKey(string privateKey, string password);

    /// <summary>
    /// Decrypts an encrypted private key using a password
    /// </summary>
    /// <param name="encryptedPrivateKey">The encrypted private key</param>
    /// <param name="password">The password to use for decryption</param>
    /// <returns>Decrypted private key</returns>
    string DecryptPrivateKey(string encryptedPrivateKey, string password);
}
