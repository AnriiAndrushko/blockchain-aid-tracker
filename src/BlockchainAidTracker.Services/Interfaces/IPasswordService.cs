namespace BlockchainAidTracker.Services.Interfaces;

/// <summary>
/// Service for password hashing and verification
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a plain text password using BCrypt
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plain text password against a hashed password
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hashedPassword">Hashed password</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool VerifyPassword(string password, string hashedPassword);
}
