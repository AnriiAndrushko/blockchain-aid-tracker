using System.Security.Cryptography;
using System.Text;
using BlockchainAidTracker.Services.Interfaces;

namespace BlockchainAidTracker.Services.Services;

/// <summary>
/// Service for managing cryptographic key encryption and decryption using AES-256
/// </summary>
public class KeyManagementService : IKeyManagementService
{
    private const int KeySize = 256; // AES-256
    private const int Iterations = 10000; // PBKDF2 iterations
    private const int SaltSize = 16; // 128 bits
    private const int IvSize = 16; // 128 bits

    /// <summary>
    /// Encrypts a private key using AES-256 with a password-derived key
    /// </summary>
    /// <param name="privateKey">The private key to encrypt</param>
    /// <param name="password">The password to use for encryption</param>
    /// <returns>Encrypted private key as base64 string (format: salt:iv:ciphertext)</returns>
    public string EncryptPrivateKey(string privateKey, string password)
    {
        if (string.IsNullOrWhiteSpace(privateKey))
        {
            throw new ArgumentException("Private key cannot be null or empty", nameof(privateKey));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        // Generate random salt and IV
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] iv = RandomNumberGenerator.GetBytes(IvSize);

        // Derive encryption key from password using PBKDF2
        using var keyDerivation = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);
        byte[] key = keyDerivation.GetBytes(KeySize / 8);

        // Encrypt the private key
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        byte[] privateKeyBytes = Encoding.UTF8.GetBytes(privateKey);
        byte[] encrypted = encryptor.TransformFinalBlock(privateKeyBytes, 0, privateKeyBytes.Length);

        // Combine salt, IV, and ciphertext
        string saltBase64 = Convert.ToBase64String(salt);
        string ivBase64 = Convert.ToBase64String(iv);
        string encryptedBase64 = Convert.ToBase64String(encrypted);

        // Format: salt:iv:ciphertext
        return $"{saltBase64}:{ivBase64}:{encryptedBase64}";
    }

    /// <summary>
    /// Decrypts an encrypted private key using AES-256 with a password-derived key
    /// </summary>
    /// <param name="encryptedPrivateKey">The encrypted private key (format: salt:iv:ciphertext)</param>
    /// <param name="password">The password to use for decryption</param>
    /// <returns>Decrypted private key</returns>
    public string DecryptPrivateKey(string encryptedPrivateKey, string password)
    {
        if (string.IsNullOrWhiteSpace(encryptedPrivateKey))
        {
            throw new ArgumentException("Encrypted private key cannot be null or empty", nameof(encryptedPrivateKey));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        try
        {
            // Parse the encrypted data (format: salt:iv:ciphertext)
            string[] parts = encryptedPrivateKey.Split(':');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Invalid encrypted private key format", nameof(encryptedPrivateKey));
            }

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] iv = Convert.FromBase64String(parts[1]);
            byte[] encrypted = Convert.FromBase64String(parts[2]);

            // Derive decryption key from password using PBKDF2
            using var keyDerivation = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256);
            byte[] key = keyDerivation.GetBytes(KeySize / 8);

            // Decrypt the private key
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

            return Encoding.UTF8.GetString(decrypted);
        }
        catch (CryptographicException)
        {
            throw new UnauthorizedAccessException("Invalid password or corrupted private key");
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid encrypted private key format", nameof(encryptedPrivateKey));
        }
    }
}
