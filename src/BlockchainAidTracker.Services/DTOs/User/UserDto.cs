using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.User;

/// <summary>
/// DTO for user information
/// </summary>
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public UserRole Role { get; set; }
    public string PublicKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
