namespace BlockchainAidTracker.Core.Interfaces;

/// <summary>
/// Service for computing cryptographic hashes.
/// </summary>
public interface IHashService
{
    /// <summary>
    /// Computes SHA-256 hash of the input string.
    /// </summary>
    /// <param name="input">The string to hash.</param>
    /// <returns>Hexadecimal string representation of the hash.</returns>
    string ComputeSha256Hash(string input);

    /// <summary>
    /// Computes SHA-256 hash of the input bytes.
    /// </summary>
    /// <param name="input">The bytes to hash.</param>
    /// <returns>Hexadecimal string representation of the hash.</returns>
    string ComputeSha256Hash(byte[] input);
}
