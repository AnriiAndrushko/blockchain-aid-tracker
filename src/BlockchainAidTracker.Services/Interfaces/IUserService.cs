using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.User;

namespace BlockchainAidTracker.Services.Interfaces;

/// <summary>
/// Service for user management operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User DTO</returns>
    Task<UserDto?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Gets a user by username
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>User DTO</returns>
    Task<UserDto?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Gets all users with optional role filter
    /// </summary>
    /// <param name="role">Optional role filter</param>
    /// <returns>List of user DTOs</returns>
    Task<List<UserDto>> GetAllUsersAsync(UserRole? role = null);

    /// <summary>
    /// Updates user profile
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated user DTO</returns>
    Task<UserDto> UpdateUserProfileAsync(string userId, UpdateUserRequest request);

    /// <summary>
    /// Assigns a role to a user (admin only)
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <param name="newRole">New role</param>
    /// <returns>Updated user DTO</returns>
    Task<UserDto> AssignRoleAsync(string userId, UserRole newRole);

    /// <summary>
    /// Deactivates a user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if deactivated successfully</returns>
    Task<bool> DeactivateUserAsync(string userId);

    /// <summary>
    /// Activates a user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if activated successfully</returns>
    Task<bool> ActivateUserAsync(string userId);
}
