using BlockchainAidTracker.Services.DTOs.Authentication;

namespace BlockchainAidTracker.Services.Interfaces;

/// <summary>
/// Service for user authentication operations
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Registers a new user
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Authentication response with tokens</returns>
    Task<AuthenticationResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Authenticates a user and returns tokens
    /// </summary>
    /// <param name="request">Login request</param>
    /// <returns>Authentication response with tokens</returns>
    Task<AuthenticationResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication response with updated tokens</returns>
    Task<AuthenticationResponse> RefreshTokenAsync(RefreshTokenRequest request);

    /// <summary>
    /// Validates a refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token to validate</param>
    /// <returns>User ID if valid, null otherwise</returns>
    Task<string?> ValidateRefreshTokenAsync(string refreshToken);
}
