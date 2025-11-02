using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.User;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Services;
using Moq;
using Xunit;

namespace BlockchainAidTracker.Tests.Services;

/// <summary>
/// Unit tests for UserService
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userService = new UserService(_userRepositoryMock.Object);
    }

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Coordinator,
            PublicKey = "publicKey",
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal("Test User", result.FullName);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByIdAsync("nonexistent", default))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetUserByIdAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUserByIdAsync_InvalidUserId_ThrowsArgumentException(string? userId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _userService.GetUserByIdAsync(userId!));
    }

    #endregion

    #region GetUserByUsernameAsync Tests

    [Fact]
    public async Task GetUserByUsernameAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        var username = "testuser";
        var user = new User
        {
            Id = "user123",
            Username = username,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Recipient,
            PublicKey = "publicKey",
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username, default))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByUsernameAsync(username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.Username);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("nonexistent", default))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetUserByUsernameAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllUsersAsync Tests

    [Fact]
    public async Task GetAllUsersAsync_NoRoleFilter_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "1", Username = "user1", FirstName = "User", LastName = "One", Role = UserRole.Recipient, CreatedTimestamp = DateTime.UtcNow, UpdatedTimestamp = DateTime.UtcNow },
            new User { Id = "2", Username = "user2", FirstName = "User", LastName = "Two", Role = UserRole.Coordinator, CreatedTimestamp = DateTime.UtcNow, UpdatedTimestamp = DateTime.UtcNow }
        };

        _userRepositoryMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(users);

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithRoleFilter_ReturnsFilteredUsers()
    {
        // Arrange
        var coordinators = new List<User>
        {
            new User { Id = "1", Username = "coordinator1", FirstName = "Coord", LastName = "One", Role = UserRole.Coordinator, CreatedTimestamp = DateTime.UtcNow, UpdatedTimestamp = DateTime.UtcNow }
        };

        _userRepositoryMock.Setup(x => x.GetByRoleAsync(UserRole.Coordinator, default))
            .ReturnsAsync(coordinators);

        // Act
        var result = await _userService.GetAllUsersAsync(UserRole.Coordinator);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(UserRole.Coordinator, result[0].Role);
    }

    #endregion

    #region UpdateUserProfileAsync Tests

    [Fact]
    public async Task UpdateUserProfileAsync_ValidRequest_UpdatesUserAndReturnsDto()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "old@example.com",
            FirstName = "Old",
            LastName = "Name",
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        var request = new UpdateUserRequest
        {
            FullName = "New Name",
            Email = "new@example.com",
            Organization = "New Org"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email!, default))
            .ReturnsAsync((User?)null); // Email not taken

        // Act
        var result = await _userService.UpdateUserProfileAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.FullName);
        Assert.Equal("new@example.com", result.Email);
        Assert.Equal("New Org", result.Organization);
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var request = new UpdateUserRequest { FullName = "New Name" };
        _userRepositoryMock.Setup(x => x.GetByIdAsync("nonexistent", default))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _userService.UpdateUserProfileAsync("nonexistent", request));
    }

    [Fact]
    public async Task UpdateUserProfileAsync_EmailAlreadyTaken_ThrowsBusinessException()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            Email = "old@example.com",
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        var request = new UpdateUserRequest
        {
            Email = "taken@example.com"
        };

        var existingUser = new User { Id = "other", Email = "taken@example.com" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email, default))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() =>
            _userService.UpdateUserProfileAsync(userId, request));
    }

    #endregion

    #region AssignRoleAsync Tests

    [Fact]
    public async Task AssignRoleAsync_ValidRequest_UpdatesRoleAndReturnsDto()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Recipient,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.AssignRoleAsync(userId, UserRole.Coordinator);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(UserRole.Coordinator, result.Role);
        _userRepositoryMock.Verify(x => x.Update(It.Is<User>(u => u.Role == UserRole.Coordinator)), Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByIdAsync("nonexistent", default))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _userService.AssignRoleAsync("nonexistent", UserRole.Administrator));
    }

    #endregion

    #region DeactivateUserAsync Tests

    [Fact]
    public async Task DeactivateUserAsync_ActiveUser_DeactivatesAndReturnsTrue()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.DeactivateUserAsync(userId);

        // Assert
        Assert.True(result);
        _userRepositoryMock.Verify(x => x.Update(It.Is<User>(u => !u.IsActive)), Times.Once);
    }

    [Fact]
    public async Task DeactivateUserAsync_AlreadyInactive_ReturnsFalse()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            IsActive = false,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.DeactivateUserAsync(userId);

        // Assert
        Assert.False(result);
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateUserAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByIdAsync("nonexistent", default))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _userService.DeactivateUserAsync("nonexistent"));
    }

    #endregion

    #region ActivateUserAsync Tests

    [Fact]
    public async Task ActivateUserAsync_InactiveUser_ActivatesAndReturnsTrue()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            IsActive = false,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.ActivateUserAsync(userId);

        // Assert
        Assert.True(result);
        _userRepositoryMock.Verify(x => x.Update(It.Is<User>(u => u.IsActive)), Times.Once);
    }

    [Fact]
    public async Task ActivateUserAsync_AlreadyActive_ReturnsFalse()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.ActivateUserAsync(userId);

        // Assert
        Assert.False(result);
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ActivateUserAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByIdAsync("nonexistent", default))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _userService.ActivateUserAsync("nonexistent"));
    }

    #endregion
}
