using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.User;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;

namespace BlockchainAidTracker.Services.Services;

/// <summary>
/// Implementation of user management service
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;

    public UserService(IUserRepository userRepository, IAuditLogService auditLogService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var user = await _userRepository.GetByIdAsync(userId);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or empty", nameof(username));
        }

        var user = await _userRepository.GetByUsernameAsync(username);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<List<UserDto>> GetAllUsersAsync(UserRole? role = null)
    {
        var users = role.HasValue
            ? await _userRepository.GetByRoleAsync(role.Value)
            : await _userRepository.GetAllAsync();

        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto> UpdateUserProfileAsync(string userId, UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found");
        }

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            // Parse full name into first and last name
            var nameParts = request.FullName.Trim().Split(' ', 2);
            user.FirstName = nameParts[0];
            user.LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            // Check if email is already taken by another user
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != userId)
            {
                throw new BusinessException($"Email '{request.Email}' is already in use");
            }
            user.Email = request.Email;
        }

        if (request.Organization != null)
        {
            user.Organization = request.Organization;
        }

        user.UpdatedTimestamp = DateTime.UtcNow;

        _userRepository.Update(user);

        // Log profile update
        await _auditLogService.LogAsync(
            AuditLogCategory.UserManagement,
            AuditLogAction.UserProfileUpdated,
            $"User profile updated for '{user.Username}'",
            userId,
            user.Username,
            userId,
            "User");

        return MapToDto(user);
    }

    public async Task<UserDto> AssignRoleAsync(string userId, UserRole newRole)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found");
        }

        var oldRole = user.Role;
        user.Role = newRole;
        user.UpdatedTimestamp = DateTime.UtcNow;

        _userRepository.Update(user);

        // Log role assignment
        await _auditLogService.LogAsync(
            AuditLogCategory.UserManagement,
            AuditLogAction.UserRoleAssigned,
            $"User '{user.Username}' role changed from {oldRole} to {newRole}",
            userId,
            user.Username,
            userId,
            "User",
            $"{{\"oldRole\":\"{oldRole}\",\"newRole\":\"{newRole}\"}}");

        return MapToDto(user);
    }

    public async Task<bool> DeactivateUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found");
        }

        if (!user.IsActive)
        {
            return false; // Already deactivated
        }

        user.Deactivate(); // Uses the method from Core model

        _userRepository.Update(user);

        // Log user deactivation
        await _auditLogService.LogAsync(
            AuditLogCategory.UserManagement,
            AuditLogAction.UserDeactivated,
            $"User '{user.Username}' deactivated",
            userId,
            user.Username,
            userId,
            "User");

        return true;
    }

    public async Task<bool> ActivateUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found");
        }

        if (user.IsActive)
        {
            return false; // Already active
        }

        user.Activate(); // Uses the method from Core model

        _userRepository.Update(user);

        // Log user activation
        await _auditLogService.LogAsync(
            AuditLogCategory.UserManagement,
            AuditLogAction.UserActivated,
            $"User '{user.Username}' activated",
            userId,
            user.Username,
            userId,
            "User");

        return true;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.GetFullName(), // Uses GetFullName() method from Core model
            Organization = user.Organization,
            Role = user.Role,
            PublicKey = user.PublicKey,
            CreatedAt = user.CreatedTimestamp,
            UpdatedAt = user.UpdatedTimestamp,
            IsActive = user.IsActive
        };
    }
}
