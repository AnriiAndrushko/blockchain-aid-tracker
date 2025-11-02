using System.Security.Claims;

namespace BlockchainAidTracker.Services.Interfaces;

/// <summary>
/// Service for JWT token generation and validation
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="username">Username</param>
    /// <param name="email">Email address</param>
    /// <param name="role">User role</param>
    /// <returns>Access token and expiration time</returns>
    (string Token, DateTime ExpiresAt) GenerateAccessToken(string userId, string username, string email, string role);

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    /// <returns>Refresh token and expiration time</returns>
    (string Token, DateTime ExpiresAt) GenerateRefreshToken();

    /// <summary>
    /// Validates a token and returns the claims principal
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Claims principal if valid, null otherwise</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Extracts user ID from a token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID if valid, null otherwise</returns>
    string? GetUserIdFromToken(string token);
}
