namespace BlockchainAidTracker.Services.DTOs.User;

/// <summary>
/// Request DTO for updating user profile
/// </summary>
public class UpdateUserRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Organization { get; set; }
}
