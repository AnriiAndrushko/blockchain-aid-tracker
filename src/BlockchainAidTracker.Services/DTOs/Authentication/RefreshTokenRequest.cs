namespace BlockchainAidTracker.Services.DTOs.Authentication;

/// <summary>
/// Request DTO for refreshing access token
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
