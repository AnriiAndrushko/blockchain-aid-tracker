using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.User;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlockchainAidTracker.Api.Controllers;

/// <summary>
/// Controller for user management operations
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        ILogger<UserController> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current authenticated user's profile
    /// </summary>
    /// <returns>Current user's profile</returns>
    /// <response code="200">Profile retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDto>> GetCurrentUserProfile()
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Retrieving profile for user {UserId}", userId);

            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return NotFound(new ProblemDetails
                {
                    Title = "User Not Found",
                    Detail = $"User with ID '{userId}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Profile retrieved successfully for user {UserId}", userId);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Updates the current authenticated user's profile
    /// </summary>
    /// <param name="request">Profile update request</param>
    /// <returns>Updated user profile</returns>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDto>> UpdateCurrentUserProfile([FromBody] UpdateUserRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Updating profile for user {UserId}", userId);

            var updatedUser = await _userService.UpdateUserProfileAsync(userId, request);

            _logger.LogInformation("Profile updated successfully for user {UserId}", userId);
            return Ok(updatedUser);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("User not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Profile update failed: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Profile Update Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets a user by ID (Admin and Coordinator access)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User profile</returns>
    /// <response code="200">User retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDto>> GetUserById(string id)
    {
        try
        {
            // Check if user has admin or coordinator role
            var currentUserRole = GetUserRoleFromClaims();
            if (currentUserRole != UserRole.Administrator && currentUserRole != UserRole.Coordinator)
            {
                var currentUserId = GetUserIdFromClaims();
                // Allow users to view their own profile
                if (currentUserId != id)
                {
                    _logger.LogWarning("User {UserId} attempted to access user {TargetId} without permission", currentUserId, id);
                    return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                    {
                        Title = "Forbidden",
                        Detail = "You do not have permission to view other users' profiles",
                        Status = StatusCodes.Status403Forbidden
                    });
                }
            }

            _logger.LogInformation("Retrieving user {UserId}", id);

            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", id);
                return NotFound(new ProblemDetails
                {
                    Title = "User Not Found",
                    Detail = $"User with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("User {UserId} retrieved successfully", id);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets all users with optional role filter (Admin access only)
    /// </summary>
    /// <param name="role">Optional role filter</param>
    /// <returns>List of users</returns>
    /// <response code="200">Users retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires administrator role</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers([FromQuery] UserRole? role = null)
    {
        try
        {
            // Only administrators can list all users
            var currentUserRole = GetUserRoleFromClaims();
            if (currentUserRole != UserRole.Administrator)
            {
                var currentUserId = GetUserIdFromClaims();
                _logger.LogWarning("User {UserId} with role {Role} attempted to list all users", currentUserId, currentUserRole);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = "Only administrators can list all users",
                    Status = StatusCodes.Status403Forbidden
                });
            }

            _logger.LogInformation("Retrieving all users with role filter: {Role}", role?.ToString() ?? "None");

            var users = await _userService.GetAllUsersAsync(role);

            _logger.LogInformation("Retrieved {Count} users", users.Count);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Assigns a role to a user (Admin access only)
    /// </summary>
    /// <param name="request">Role assignment request</param>
    /// <returns>Updated user profile</returns>
    /// <response code="200">Role assigned successfully</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires administrator role</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("assign-role")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDto>> AssignRole([FromBody] AssignRoleRequest request)
    {
        try
        {
            // Only administrators can assign roles
            var currentUserRole = GetUserRoleFromClaims();
            if (currentUserRole != UserRole.Administrator)
            {
                var currentUserId = GetUserIdFromClaims();
                _logger.LogWarning("User {UserId} with role {Role} attempted to assign role", currentUserId, currentUserRole);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = "Only administrators can assign roles",
                    Status = StatusCodes.Status403Forbidden
                });
            }

            _logger.LogInformation("Assigning role {Role} to user {UserId}", request.Role, request.UserId);

            var updatedUser = await _userService.AssignRoleAsync(request.UserId, request.Role);

            _logger.LogInformation("Role {Role} assigned successfully to user {UserId}", request.Role, request.UserId);
            return Ok(updatedUser);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("User not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Role assignment failed: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Role Assignment Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user {UserId}", request.UserId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Deactivates a user account (Admin access only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success message</returns>
    /// <response code="200">User deactivated successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires administrator role</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeactivateUser(string id)
    {
        try
        {
            // Only administrators can deactivate users
            var currentUserRole = GetUserRoleFromClaims();
            if (currentUserRole != UserRole.Administrator)
            {
                var currentUserId = GetUserIdFromClaims();
                _logger.LogWarning("User {UserId} with role {Role} attempted to deactivate user", currentUserId, currentUserRole);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = "Only administrators can deactivate users",
                    Status = StatusCodes.Status403Forbidden
                });
            }

            _logger.LogInformation("Deactivating user {UserId}", id);

            var success = await _userService.DeactivateUserAsync(id);

            if (!success)
            {
                _logger.LogWarning("User {UserId} not found for deactivation", id);
                return NotFound(new ProblemDetails
                {
                    Title = "User Not Found",
                    Detail = $"User with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("User {UserId} deactivated successfully", id);
            return Ok(new { message = $"User with ID '{id}' has been deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Activates a user account (Admin access only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success message</returns>
    /// <response code="200">User activated successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires administrator role</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ActivateUser(string id)
    {
        try
        {
            // Only administrators can activate users
            var currentUserRole = GetUserRoleFromClaims();
            if (currentUserRole != UserRole.Administrator)
            {
                var currentUserId = GetUserIdFromClaims();
                _logger.LogWarning("User {UserId} with role {Role} attempted to activate user", currentUserId, currentUserRole);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = "Only administrators can activate users",
                    Status = StatusCodes.Status403Forbidden
                });
            }

            _logger.LogInformation("Activating user {UserId}", id);

            var success = await _userService.ActivateUserAsync(id);

            if (!success)
            {
                _logger.LogWarning("User {UserId} not found for activation", id);
                return NotFound(new ProblemDetails
                {
                    Title = "User Not Found",
                    Detail = $"User with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("User {UserId} activated successfully", id);
            return Ok(new { message = $"User with ID '{id}' has been activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Helper method to extract user ID from JWT claims
    /// </summary>
    private string GetUserIdFromClaims()
    {
        return User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedException("User ID not found in token");
    }

    /// <summary>
    /// Helper method to extract user role from JWT claims
    /// </summary>
    private UserRole GetUserRoleFromClaims()
    {
        var roleString = User.FindFirst("role")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            ?? throw new UnauthorizedException("User role not found in token");

        if (!Enum.TryParse<UserRole>(roleString, true, out var role))
        {
            throw new UnauthorizedException($"Invalid role value: {roleString}");
        }

        return role;
    }
}
