namespace BlockchainAidTracker.Services.Configuration;

/// <summary>
/// Configuration settings for JWT token generation
/// </summary>
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "BlockchainAidTracker";
    public string Audience { get; set; } = "BlockchainAidTracker";
    public int AccessTokenExpirationMinutes { get; set; } = 60; // 1 hour
    public int RefreshTokenExpirationDays { get; set; } = 7; // 7 days
}
