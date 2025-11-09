namespace BlockchainAidTracker.Services.DTOs.Authentication;

/// <summary>
/// Request DTO for user registration
/// </summary>
public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string Role { get; set; } = string.Empty;
}
