using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.Authentication;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;
using BlockchainAidTracker.Services.Services;
using Moq;
using Xunit;

namespace BlockchainAidTracker.Tests.Services;

/// <summary>
/// Unit tests for AuthenticationService
/// </summary>
public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IDigitalSignatureService> _digitalSignatureServiceMock;
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _digitalSignatureServiceMock = new Mock<IDigitalSignatureService>();

        _authService = new AuthenticationService(
            _userRepositoryMock.Object,
            _passwordServiceMock.Object,
            _tokenServiceMock.Object,
            _digitalSignatureServiceMock.Object
        );
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsAuthenticationResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123!",
            FullName = "Test User",
            Organization = "Test Org"
        };

        _userRepositoryMock.Setup(x => x.UsernameExistsAsync(request.Username, default))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(request.Email, default))
            .ReturnsAsync(false);
        _digitalSignatureServiceMock.Setup(x => x.GenerateKeyPair())
            .Returns(("publicKey", "privateKey"));
        _passwordServiceMock.Setup(x => x.HashPassword(request.Password))
            .Returns("hashedPassword");
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("accessToken", DateTime.UtcNow.AddHours(1)));
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns(("refreshToken", DateTime.UtcNow.AddDays(7)));
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .ReturnsAsync((User u, CancellationToken ct) => u);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("accessToken", result.AccessToken);
        Assert.Equal("refreshToken", result.RefreshToken);
        Assert.Equal(request.Username, result.Username);
        Assert.Equal(request.Email, result.Email);
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), default), Times.Once);
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _authService.RegisterAsync(null!));
    }

    [Theory]
    [InlineData("", "test@example.com", "Password123!", "Test User")]
    [InlineData("testuser", "", "Password123!", "Test User")]
    [InlineData("testuser", "test@example.com", "", "Test User")]
    [InlineData("testuser", "test@example.com", "Password123!", "")]
    public async Task RegisterAsync_MissingRequiredFields_ThrowsArgumentException(
        string username, string email, string password, string fullName)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password,
            FullName = fullName
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_PasswordTooShort_ThrowsArgumentException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Pass1!",  // Less than 8 characters
            FullName = "Test User"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_InvalidEmail_ThrowsArgumentException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "notanemail",  // No @ symbol
            Password = "Password123!",
            FullName = "Test User"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_UsernameAlreadyExists_ThrowsBusinessException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "existinguser",
            Email = "test@example.com",
            Password = "Password123!",
            FullName = "Test User"
        };

        _userRepositoryMock.Setup(x => x.UsernameExistsAsync(request.Username, default))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _authService.RegisterAsync(request));
        Assert.Contains("already taken", exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ThrowsBusinessException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "existing@example.com",
            Password = "Password123!",
            FullName = "Test User"
        };

        _userRepositoryMock.Setup(x => x.UsernameExistsAsync(request.Username, default))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(request.Email, default))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _authService.RegisterAsync(request));
        Assert.Contains("already registered", exception.Message);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthenticationResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            UsernameOrEmail = "testuser",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = "user123",
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashedPassword",
            Role = UserRole.Coordinator,
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameOrEmailAsync(request.UsernameOrEmail, default))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user.Id, user.Username, user.Email, user.Role.ToString()))
            .Returns(("accessToken", DateTime.UtcNow.AddHours(1)));
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns(("refreshToken", DateTime.UtcNow.AddDays(7)));

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("accessToken", result.AccessToken);
        Assert.Equal("refreshToken", result.RefreshToken);
        Assert.Equal(user.Username, result.Username);
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest
        {
            UsernameOrEmail = "nonexistent",
            Password = "Password123!"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameOrEmailAsync(request.UsernameOrEmail, default))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(request));
        Assert.Contains("Invalid username/email or password", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_IncorrectPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest
        {
            UsernameOrEmail = "testuser",
            Password = "WrongPassword!"
        };

        var user = new User
        {
            Id = "user123",
            Username = "testuser",
            PasswordHash = "hashedPassword",
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameOrEmailAsync(request.UsernameOrEmail, default))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(request));
        Assert.Contains("Invalid username/email or password", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest
        {
            UsernameOrEmail = "testuser",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = "user123",
            Username = "testuser",
            PasswordHash = "hashedPassword",
            IsActive = false
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameOrEmailAsync(request.UsernameOrEmail, default))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(request));
        Assert.Contains("inactive", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _authService.LoginAsync(null!));
    }

    [Theory]
    [InlineData("", "Password123!")]
    [InlineData("testuser", "")]
    public async Task LoginAsync_MissingCredentials_ThrowsArgumentException(string usernameOrEmail, string password)
    {
        // Arrange
        var request = new LoginRequest
        {
            UsernameOrEmail = usernameOrEmail,
            Password = password
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(request));
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_ValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "validRefreshToken"
        };

        var user = new User
        {
            Id = "user123",
            Username = "testuser",
            Email = "test@example.com",
            Role = UserRole.Coordinator,
            IsActive = true,
            RefreshToken = "validRefreshToken",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _userRepositoryMock.Setup(x => x.GetByRefreshTokenAsync(request.RefreshToken, default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, default))
            .ReturnsAsync(user);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user.Id, user.Username, user.Email, user.Role.ToString()))
            .Returns(("newAccessToken", DateTime.UtcNow.AddHours(1)));
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns(("newRefreshToken", DateTime.UtcNow.AddDays(7)));

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newAccessToken", result.AccessToken);
        Assert.Equal("newRefreshToken", result.RefreshToken);
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidRefreshToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalidToken"
        };

        _userRepositoryMock.Setup(x => x.GetByRefreshTokenAsync(request.RefreshToken, default))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.RefreshTokenAsync(request));
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredRefreshToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "expiredToken"
        };

        var user = new User
        {
            Id = "user123",
            RefreshToken = "expiredToken",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired
        };

        _userRepositoryMock.Setup(x => x.GetByRefreshTokenAsync(request.RefreshToken, default))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.RefreshTokenAsync(request));
    }

    [Fact]
    public async Task RefreshTokenAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _authService.RefreshTokenAsync(null!));
    }

    #endregion

    #region ValidateRefreshTokenAsync Tests

    [Fact]
    public async Task ValidateRefreshTokenAsync_ValidToken_ReturnsUserId()
    {
        // Arrange
        var refreshToken = "validToken";
        var user = new User
        {
            Id = "user123",
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _userRepositoryMock.Setup(x => x.GetByRefreshTokenAsync(refreshToken, default))
            .ReturnsAsync(user);

        // Act
        var userId = await _authService.ValidateRefreshTokenAsync(refreshToken);

        // Assert
        Assert.Equal(user.Id, userId);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_TokenNotFound_ReturnsNull()
    {
        // Arrange
        var refreshToken = "nonExistentToken";
        _userRepositoryMock.Setup(x => x.GetByRefreshTokenAsync(refreshToken, default))
            .ReturnsAsync((User?)null);

        // Act
        var userId = await _authService.ValidateRefreshTokenAsync(refreshToken);

        // Assert
        Assert.Null(userId);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ExpiredToken_ReturnsNull()
    {
        // Arrange
        var refreshToken = "expiredToken";
        var user = new User
        {
            Id = "user123",
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        _userRepositoryMock.Setup(x => x.GetByRefreshTokenAsync(refreshToken, default))
            .ReturnsAsync(user);

        // Act
        var userId = await _authService.ValidateRefreshTokenAsync(refreshToken);

        // Assert
        Assert.Null(userId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateRefreshTokenAsync_InvalidTokenString_ReturnsNull(string? token)
    {
        // Act
        var userId = await _authService.ValidateRefreshTokenAsync(token!);

        // Assert
        Assert.Null(userId);
    }

    #endregion
}
