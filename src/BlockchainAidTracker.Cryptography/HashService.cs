using System.Security.Cryptography;
using System.Text;
using BlockchainAidTracker.Core.Interfaces;

namespace BlockchainAidTracker.Cryptography;

/// <summary>
/// Implementation of cryptographic hashing service using SHA-256.
/// </summary>
public class HashService : IHashService
{
    /// <summary>
    /// Computes SHA-256 hash of the input string.
    /// </summary>
    public string ComputeSha256Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input cannot be null or empty.", nameof(input));
        }

        var bytes = Encoding.UTF8.GetBytes(input);
        return ComputeSha256Hash(bytes);
    }

    /// <summary>
    /// Computes SHA-256 hash of the input bytes.
    /// </summary>
    public string ComputeSha256Hash(byte[] input)
    {
        if (input == null || input.Length == 0)
        {
            throw new ArgumentException("Input cannot be null or empty.", nameof(input));
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(input);

        // Convert to hexadecimal string
        return Convert.ToHexString(hashBytes);
    }
}
