using System.Collections.Concurrent;

namespace BlockchainAidTracker.Services.Services;

/// <summary>
/// Thread-safe in-memory context for storing decrypted private keys during user sessions
/// NOTE: In production, this should be replaced with a secure key management solution (HSM, Azure Key Vault, etc.)
/// </summary>
public class TransactionSigningContext
{
    private readonly ConcurrentDictionary<string, string> _privateKeys = new();

    /// <summary>
    /// Stores a decrypted private key for a user session
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="privateKey">Decrypted private key</param>
    public void StorePrivateKey(string userId, string privateKey)
    {
        _privateKeys[userId] = privateKey;
    }

    /// <summary>
    /// Retrieves a decrypted private key for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Private key if available, null otherwise</returns>
    public string? GetPrivateKey(string userId)
    {
        return _privateKeys.TryGetValue(userId, out var key) ? key : null;
    }

    /// <summary>
    /// Removes a private key from the context (e.g., on logout)
    /// </summary>
    /// <param name="userId">User ID</param>
    public void RemovePrivateKey(string userId)
    {
        _privateKeys.TryRemove(userId, out _);
    }

    /// <summary>
    /// Checks if a private key is available for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if private key is available</returns>
    public bool HasPrivateKey(string userId)
    {
        return _privateKeys.ContainsKey(userId);
    }
}
