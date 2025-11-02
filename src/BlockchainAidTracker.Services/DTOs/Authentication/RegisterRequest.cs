namespace BlockchainAidTracker.Services.DTOs.Authentication;

/// <summary>
/// Request DTO for user registration
/// </summary>
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Organization { get; set; }
}
