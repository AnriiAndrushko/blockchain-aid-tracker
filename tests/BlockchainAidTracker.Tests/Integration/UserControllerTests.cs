using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.Authentication;
using BlockchainAidTracker.Services.DTOs.User;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BlockchainAidTracker.Tests.Integration;

/// <summary>
/// Integration tests for UserController
/// </summary>
public class UserControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UserControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a user via registration and returns the auth token
    /// </summary>
    private async Task<(string Token, string UserId)> CreateUserAsync(string username, string email, string password = "SecurePassword123!")
    {
        var registerRequest = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password,
            FullName = $"{username} User",
            Organization = "Test Organization"
        };

        var response = await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();

        return (authResponse!.AccessToken, authResponse.UserId);
    }

    /// <summary>
    /// Creates a user with a specific role using database access
    /// </summary>
    private async Task<(string Token, string UserId)> CreateUserWithRoleAsync(string username, string email, UserRole role, string password = "SecurePassword123!")
    {
        // Create user via registration (default role is Recipient)
        var (token, userId) = await CreateUserAsync(username, email, password);

        // If a different role is needed, use direct database access to update the role
        if (role != UserRole.Recipient)
        {
            using var scope = _factory.Services.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<BlockchainAidTracker.DataAccess.Repositories.IUserRepository>();
            var user = await userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.Role = role;
                userRepository.Update(user);
            }
        }

        return (token, userId);
    }

    #endregion

    #region GET /api/users/profile

    [Fact]
    public async Task GetCurrentUserProfile_WithValidToken_ReturnsOk()
    {
        // Arrange
        var (token, userId) = await CreateUserAsync("profileuser", "profileuser@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/users/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Id.Should().Be(userId);
        userDto.Username.Should().Be("profileuser");
        userDto.Email.Should().Be("profileuser@example.com");
        userDto.FullName.Should().Be("profileuser User");
        userDto.Organization.Should().Be("Test Organization");
        userDto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUserProfile_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/users/profile

    [Fact]
    public async Task UpdateCurrentUserProfile_WithValidData_ReturnsOk()
    {
        // Arrange
        var (token, userId) = await CreateUserAsync("updateuser", "updateuser@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateUserRequest
        {
            FullName = "Updated Name",
            Email = "updated@example.com",
            Organization = "Updated Organization"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.FullName.Should().Be("Updated Name");
        userDto.Email.Should().Be("updated@example.com");
        userDto.Organization.Should().Be("Updated Organization");
        userDto.Username.Should().Be("updateuser"); // Username should not change
    }

    [Fact]
    public async Task UpdateCurrentUserProfile_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        // Create first user
        await CreateUserAsync("user1", "user1@example.com");

        // Create second user
        var (token, _) = await CreateUserAsync("user2", "user2@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Try to update to user1's email
        var updateRequest = new UpdateUserRequest
        {
            Email = "user1@example.com"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCurrentUserProfile_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var updateRequest = new UpdateUserRequest
        {
            FullName = "New Name"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/users/{id}

    [Fact]
    public async Task GetUserById_AsAdministrator_ReturnsOk()
    {
        // Arrange
        var (adminToken, _) = await CreateUserWithRoleAsync("admin", "admin@example.com", UserRole.Administrator);
        var (_, targetUserId) = await CreateUserAsync("targetuser", "target@example.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync($"/api/users/{targetUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Id.Should().Be(targetUserId);
        userDto.Username.Should().Be("targetuser");
    }

    [Fact]
    public async Task GetUserById_AsCoordinator_ReturnsOk()
    {
        // Arrange
        var (coordinatorToken, _) = await CreateUserWithRoleAsync("coordinator", "coordinator@example.com", UserRole.Coordinator);
        var (_, targetUserId) = await CreateUserAsync("targetuser2", "target2@example.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);

        // Act
        var response = await _client.GetAsync($"/api/users/{targetUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Id.Should().Be(targetUserId);
    }

    [Fact]
    public async Task GetUserById_OwnProfile_ReturnsOk()
    {
        // Arrange
        var (token, userId) = await CreateUserAsync("ownprofile", "ownprofile@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetUserById_AsRecipientViewingOther_ReturnsForbidden()
    {
        // Arrange
        var (recipientToken, _) = await CreateUserAsync("recipient", "recipient@example.com");
        var (_, targetUserId) = await CreateUserAsync("otheruser", "other@example.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);

        // Act
        var response = await _client.GetAsync($"/api/users/{targetUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserById_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var (adminToken, _) = await CreateUserWithRoleAsync("admin2", "admin2@example.com", UserRole.Administrator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/users/nonexistent-id-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserById_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users/some-user-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/users

    [Fact]
    public async Task GetAllUsers_AsAdministrator_ReturnsOk()
    {
        // Arrange
        var (adminToken, _) = await CreateUserWithRoleAsync("admin3", "admin3@example.com", UserRole.Administrator);
        await CreateUserAsync("listuser1", "listuser1@example.com");
        await CreateUserAsync("listuser2", "listuser2@example.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Should().HaveCountGreaterThanOrEqualTo(3); // At least admin + 2 test users
    }

    [Fact]
    public async Task GetAllUsers_WithRoleFilter_ReturnsFilteredUsers()
    {
        // Arrange
        var (adminToken, _) = await CreateUserWithRoleAsync("admin4", "admin4@example.com", UserRole.Administrator);
        await CreateUserWithRoleAsync("coord1", "coord1@example.com", UserRole.Coordinator);
        await CreateUserWithRoleAsync("coord2", "coord2@example.com", UserRole.Coordinator);
        await CreateUserAsync("recip1", "recip1@example.com"); // Recipient

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync($"/api/users?role={UserRole.Coordinator}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Should().HaveCountGreaterThanOrEqualTo(2);
        users.Should().OnlyContain(u => u.Role == UserRole.Coordinator);
    }

    [Fact]
    public async Task GetAllUsers_AsNonAdministrator_ReturnsForbidden()
    {
        // Arrange
        var (coordinatorToken, _) = await CreateUserWithRoleAsync("coord3", "coord3@example.com", UserRole.Coordinator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);

        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllUsers_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/users/assign-role

    [Fact]
    public async Task AssignRole_AsAdministrator_ReturnsOk()
    {
        // Arrange
        var (adminToken, _) = await CreateUserWithRoleAsync("admin5", "admin5@example.com", UserRole.Administrator);
        var (_, targetUserId) = await CreateUserAsync("roleuser", "roleuser@example.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var assignRoleRequest = new AssignRoleRequest
        {
            UserId = targetUserId,
            Role = UserRole.Coordinator
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/assign-role", assignRoleRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Role.Should().Be(UserRole.Coordinator);
        userDto.Id.Should().Be(targetUserId);
    }

    [Fact]
    public async Task AssignRole_AsNonAdministrator_ReturnsForbidden()
    {
        // Arrange
        var (coordinatorToken, _) = await CreateUserWithRoleAsync("coord4", "coord4@example.com", UserRole.Coordinator);
        var (_, targetUserId) = await CreateUserAsync("roleuser2", "roleuser2@example.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);

        var assignRoleRequest = new AssignRoleRequest
        {
            UserId = targetUserId,
            Role = UserRole.Administrator
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/assign-role", assignRoleRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_ToNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var (adminToken, _) = await CreateUserWithRoleAsync("admin6", "admin6@example.com", UserRole.Administrator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var assignRoleRequest = new AssignRoleRequest
        {
            UserId = "nonexistent-user-id",
            Role = UserRole.Coordinator
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/assign-role", assignRoleRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignRole_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var assignRoleRequest = new AssignRoleRequest
        {
            UserId = "some-user-id",
            Role = UserRole.Coordinator
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/assign-role", assignRoleRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/users/{id}/deactivate

    [Fact]
    public async Task DeactivateUser_AsAdministrator_ReturnsOk()
    {
        // Arrange
        var (adminToken, _) = await CreateUserWithRoleAsync("admin7", "admin7@example.com", UserRole.Administrator);
        var (_, targetUserId) = await CreateUserAsync("deactivateuser", "deactivate@example.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsync($"/api/users/{targetUserId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        result.Should().NotBeNull();
        result!["message"].Should().Contain("deactivated successfully");
    }

    [Fact]
    public async Task DeactivateUser_AsNonAdministrator_ReturnsForbidden()
    {
        // Arrange
        var (coordinatorToken, _) = await CreateUserWithRoleAsync("coord5", "coord5@example.com", UserRole.Coordinator);
        var (_, targetUserId) = await CreateUserAsync("deactivateuser2", "deactivate2@example.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);

        // Act
        var response = await _client.PostAsync($"/api/users/{targetUserId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateUser_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var (adminToken, _) = await CreateUserWithRoleAsync("admin8", "admin8@example.com", UserRole.Administrator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsync("/api/users/nonexistent-id/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateUser_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/users/some-user-id/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/users/{id}/activate

    [Fact]
    public async Task ActivateUser_AsAdministrator_ReturnsOk()
    {
        // Arrange
        var (adminToken, _) = await CreateUserWithRoleAsync("admin9", "admin9@example.com", UserRole.Administrator);
        var (_, targetUserId) = await CreateUserAsync("activateuser", "activate@example.com");

        // First deactivate the user
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.PostAsync($"/api/users/{targetUserId}/deactivate", null);

        // Act - Now activate the user
        var response = await _client.PostAsync($"/api/users/{targetUserId}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        result.Should().NotBeNull();
        result!["message"].Should().Contain("activated successfully");
    }

    [Fact]
    public async Task ActivateUser_AsNonAdministrator_ReturnsForbidden()
    {
        // Arrange
        var (coordinatorToken, _) = await CreateUserWithRoleAsync("coord6", "coord6@example.com", UserRole.Coordinator);
        var (_, targetUserId) = await CreateUserAsync("activateuser2", "activate2@example.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);

        // Act
        var response = await _client.PostAsync($"/api/users/{targetUserId}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateUser_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var (adminToken, _) = await CreateUserWithRoleAsync("admin10", "admin10@example.com", UserRole.Administrator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsync("/api/users/nonexistent-id/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActivateUser_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/users/some-user-id/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region End-to-End Workflow

    [Fact]
    public async Task UserManagementWorkflow_CompleteLifecycle_WorksCorrectly()
    {
        // Arrange
        var (adminToken, _) = await CreateUserWithRoleAsync("workflow-admin", "workflow-admin@example.com", UserRole.Administrator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Step 1: Create a new user
        var (userToken, userId) = await CreateUserAsync("workflow-user", "workflow-user@example.com");

        // Step 2: Get user profile (as the user)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var profileResponse = await _client.GetAsync("/api/users/profile");
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await profileResponse.Content.ReadFromJsonAsync<UserDto>();
        profile!.Username.Should().Be("workflow-user");
        profile.Role.Should().Be(UserRole.Recipient); // Default role

        // Step 3: Update profile (as the user)
        var updateRequest = new UpdateUserRequest
        {
            FullName = "Updated Workflow User",
            Organization = "Workflow Org"
        };
        var updateResponse = await _client.PutAsJsonAsync("/api/users/profile", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProfile = await updateResponse.Content.ReadFromJsonAsync<UserDto>();
        updatedProfile!.FullName.Should().Be("Updated Workflow User");
        updatedProfile.Organization.Should().Be("Workflow Org");

        // Step 4: Assign role (as admin)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var assignRoleRequest = new AssignRoleRequest
        {
            UserId = userId,
            Role = UserRole.Coordinator
        };
        var assignResponse = await _client.PostAsJsonAsync("/api/users/assign-role", assignRoleRequest);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var roleUpdated = await assignResponse.Content.ReadFromJsonAsync<UserDto>();
        roleUpdated!.Role.Should().Be(UserRole.Coordinator);

        // Step 5: Get user by ID (as admin)
        var getUserResponse = await _client.GetAsync($"/api/users/{userId}");
        getUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedUser = await getUserResponse.Content.ReadFromJsonAsync<UserDto>();
        fetchedUser!.Role.Should().Be(UserRole.Coordinator);

        // Step 6: List all users (as admin)
        var listResponse = await _client.GetAsync("/api/users");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await listResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().Contain(u => u.Id == userId);

        // Step 7: Deactivate user (as admin)
        var deactivateResponse = await _client.PostAsync($"/api/users/{userId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 8: Verify user is deactivated
        var checkUserResponse = await _client.GetAsync($"/api/users/{userId}");
        var deactivatedUser = await checkUserResponse.Content.ReadFromJsonAsync<UserDto>();
        deactivatedUser!.IsActive.Should().BeFalse();

        // Step 9: Reactivate user (as admin)
        var activateResponse = await _client.PostAsync($"/api/users/{userId}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 10: Verify user is reactivated
        var finalUserResponse = await _client.GetAsync($"/api/users/{userId}");
        var reactivatedUser = await finalUserResponse.Content.ReadFromJsonAsync<UserDto>();
        reactivatedUser!.IsActive.Should().BeTrue();
    }

    #endregion
}
