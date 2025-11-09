using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlockchainAidTracker.Services.DTOs.Authentication;
using FluentAssertions;

namespace BlockchainAidTracker.Tests.Integration;

/// <summary>
/// Integration tests for AuthenticationController
/// </summary>
public class AuthenticationControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthenticationControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsOkWithAuthenticationResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Username = "testuser",
            Email = "testuser@example.com",
            Password = "SecurePassword123!",
            Organization = "Test Organization",
            Role = "Recipient"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("testuser@example.com");
        result.UserId.Should().NotBeNullOrEmpty();
        result.Role.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "User",
            LastName = "One",
            Username = "duplicateuser",
            Email = "user1@example.com",
            Password = "SecurePassword123!",
            Role = "Recipient"
        };

        // Register first user
        await _client.PostAsJsonAsync("/api/authentication/register", request);

        // Try to register with same username but different email
        var duplicateRequest = new RegisterRequest
        {
            FirstName = "User",
            LastName = "Two",
            Username = "duplicateuser",
            Email = "user2@example.com",
            Password = "SecurePassword123!",
            Role = "Recipient"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/register", duplicateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "User",
            LastName = "One",
            Username = "user1",
            Email = "duplicate@example.com",
            Password = "SecurePassword123!",
            Role = "Recipient"
        };

        // Register first user
        await _client.PostAsJsonAsync("/api/authentication/register", request);

        // Try to register with different username but same email
        var duplicateRequest = new RegisterRequest
        {
            FirstName = "User",
            LastName = "Two",
            Username = "user2",
            Email = "duplicate@example.com",
            Password = "SecurePassword123!",
            Role = "Recipient"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/register", duplicateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithEmptyUsername_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Username = "",
            Email = "test@example.com",
            Password = "SecurePassword123!",
            Role = "Recipient"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Username = "testuser",
            Email = "test@example.com",
            Password = "short",
            Role = "Recipient"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithAuthenticationResponse()
    {
        // Arrange - Register a user first
        var registerRequest = new RegisterRequest
        {
            FirstName = "Login",
            LastName = "User",
            Username = "loginuser",
            Email = "loginuser@example.com",
            Password = "SecurePassword123!",
            Role = "Recipient"
        };
        await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "loginuser",
            Password = "SecurePassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("loginuser");
    }

    [Fact]
    public async Task Login_WithEmail_ReturnsOkWithAuthenticationResponse()
    {
        // Arrange - Register a user first
        var registerRequest = new RegisterRequest
        {
            FirstName = "Email",
            LastName = "User",
            Username = "emailuser",
            Email = "emaillogin@example.com",
            Password = "SecurePassword123!",
            Role = "Recipient"
        };
        await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "emaillogin@example.com",
            Password = "SecurePassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        result.Should().NotBeNull();
        result!.Username.Should().Be("emailuser");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange - Register a user first
        var registerRequest = new RegisterRequest
        {
            FirstName = "Password",
            LastName = "Test",
            Username = "passwordtest",
            Email = "passwordtest@example.com",
            Password = "SecurePassword123!",
            Role = "Recipient"
        };
        await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "passwordtest",
            Password = "WrongPassword!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "nonexistent",
            Password = "SecurePassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ReturnsOkWithNewTokens()
    {
        // Arrange - Register and login to get tokens
        var registerRequest = new RegisterRequest
        {
            FirstName = "Refresh",
            LastName = "User",
            Username = "refreshuser",
            Email = "refreshuser@example.com",
            Password = "SecurePassword123!",
            Role = "Recipient"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = authResponse!.RefreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/refresh-token", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.AccessToken.Should().NotBe(authResponse.AccessToken); // Should be a new token
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/refresh-token", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        // Arrange - Register and login to get token
        var registerRequest = new RegisterRequest
        {
            FirstName = "Logout",
            LastName = "User",
            Username = "logoutuser",
            Email = "logoutuser@example.com",
            Password = "SecurePassword123!",
            Role = "Recipient"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Act
        var response = await _client.PostAsync("/api/authentication/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/authentication/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidateToken_WithValidToken_ReturnsOk()
    {
        // Arrange - Register to get token
        var registerRequest = new RegisterRequest
        {
            FirstName = "Validate",
            LastName = "User",
            Username = "validateuser",
            Email = "validateuser@example.com",
            Password = "SecurePassword123!",
            Role = "Recipient"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/authentication/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidateToken_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/authentication/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidateToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/api/authentication/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullAuthenticationFlow_RegisterLoginRefreshLogout_AllSucceed()
    {
        // 1. Register
        var registerRequest = new RegisterRequest
        {
            FirstName = "Flow",
            LastName = "User",
            Username = "flowuser",
            Email = "flowuser@example.com",
            Password = "SecurePassword123!",
            Role = "Recipient"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        // 2. Login
        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "flowuser",
            Password = "SecurePassword123!"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        // 3. Refresh Token
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResult!.RefreshToken
        };
        var refreshResponse = await _client.PostAsJsonAsync("/api/authentication/refresh-token", refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        // 4. Validate Token
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshResult!.AccessToken);
        var validateResponse = await _client.GetAsync("/api/authentication/validate");
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Logout
        var logoutResponse = await _client.PostAsync("/api/authentication/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
