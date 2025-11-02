using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.User;

/// <summary>
/// Request DTO for assigning role to a user
/// </summary>
public class AssignRoleRequest
{
    public string UserId { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
