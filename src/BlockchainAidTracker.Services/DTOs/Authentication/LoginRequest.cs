namespace BlockchainAidTracker.Services.DTOs.Authentication;

/// <summary>
/// Request DTO for user login
/// </summary>
public class LoginRequest
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
