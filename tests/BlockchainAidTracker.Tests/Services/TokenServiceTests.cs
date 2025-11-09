using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BlockchainAidTracker.Services.Configuration;
using BlockchainAidTracker.Services.Services;
using Xunit;

namespace BlockchainAidTracker.Tests.Services;

/// <summary>
/// Unit tests for TokenService
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public TokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "ThisIsAVerySecureSecretKeyForTestingPurposesOnly123456789",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        _tokenService = new TokenService(_jwtSettings);
    }

    [Fact]
    public void Constructor_NullJwtSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TokenService(null!));
    }

    [Fact]
    public void Constructor_EmptySecretKey_ThrowsArgumentException()
    {
        // Arrange
        var invalidSettings = new JwtSettings { SecretKey = "" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TokenService(invalidSettings));
    }

    [Fact]
    public void GenerateAccessToken_ValidInput_ReturnsTokenAndExpiration()
    {
        // Arrange
        var userId = "user123";
        var username = "testuser";
        var email = "test@example.com";
        var role = "Coordinator";
        var firstName = "Test";
        var lastName = "User";

        // Act
        var (token, expiresAt) = _tokenService.GenerateAccessToken(userId, username, email, role, firstName, lastName);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.True(expiresAt > DateTime.UtcNow);
        Assert.True(expiresAt <= DateTime.UtcNow.AddMinutes(61)); // Allow 1 minute tolerance
    }

    [Theory]
    [InlineData(null, "username", "email@test.com", "role")]
    [InlineData("", "username", "email@test.com", "role")]
    [InlineData("   ", "username", "email@test.com", "role")]
    public void GenerateAccessToken_InvalidUserId_ThrowsArgumentException(string? userId, string username, string email, string role)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _tokenService.GenerateAccessToken(userId!, username, email, role, "Test", "User"));
    }

    [Fact]
    public void GenerateAccessToken_TokenContainsExpectedClaims()
    {
        // Arrange
        var userId = "user123";
        var username = "testuser";
        var email = "test@example.com";
        var role = "Coordinator";
        var firstName = "Test";
        var lastName = "User";

        // Act
        var (token, _) = _tokenService.GenerateAccessToken(userId, username, email, role, firstName, lastName);

        // Assert - Parse token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(userId, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(username, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal(email, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(role, jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal(firstName, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.GivenName).Value);
        Assert.Equal(lastName, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.FamilyName).Value);
        Assert.NotNull(jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsTokenAndExpiration()
    {
        // Act
        var (token, expiresAt) = _tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.True(expiresAt > DateTime.UtcNow);
        Assert.True(expiresAt <= DateTime.UtcNow.AddDays(8)); // Allow 1 day tolerance
    }

    [Fact]
    public void GenerateRefreshToken_GeneratesUniqueTokens()
    {
        // Act
        var (token1, _) = _tokenService.GenerateRefreshToken();
        var (token2, _) = _tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsPrincipal()
    {
        // Arrange
        var userId = "user123";
        var username = "testuser";
        var email = "test@example.com";
        var role = "Coordinator";
        var firstName = "Test";
        var lastName = "User";
        var (token, _) = _tokenService.GenerateAccessToken(userId, username, email, role, firstName, lastName);

        // Act
        var principal = _tokenService.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        // The claim type might be the full URI or short name, check both
        var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub) ?? principal.FindFirst("sub");
        Assert.NotNull(subClaim);
        Assert.Equal(userId, subClaim.Value);
        Assert.Equal(username, principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value);
        Assert.Equal(email, principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value);
        Assert.Equal(role, principal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.Equal(firstName, principal.FindFirst(JwtRegisteredClaimNames.GivenName)?.Value);
        Assert.Equal(lastName, principal.FindFirst(JwtRegisteredClaimNames.FamilyName)?.Value);
    }

    [Fact]
    public void ValidateToken_NullToken_ReturnsNull()
    {
        // Act
        var principal = _tokenService.ValidateToken(null!);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_EmptyToken_ReturnsNull()
    {
        // Act
        var principal = _tokenService.ValidateToken("");

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        // Act
        var principal = _tokenService.ValidateToken("invalid.token.string");

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_TamperedToken_ReturnsNull()
    {
        // Arrange
        var userId = "user123";
        var (token, _) = _tokenService.GenerateAccessToken(userId, "testuser", "test@example.com", "Coordinator", "Test", "User");
        var tamperedToken = token.Substring(0, token.Length - 5) + "AAAAA"; // Tamper with signature

        // Act
        var principal = _tokenService.ValidateToken(tamperedToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetUserIdFromToken_ValidToken_ReturnsUserId()
    {
        // Arrange
        var userId = "user123";
        var (token, _) = _tokenService.GenerateAccessToken(userId, "testuser", "test@example.com", "Coordinator", "Test", "User");

        // Act
        var extractedUserId = _tokenService.GetUserIdFromToken(token);

        // Assert
        Assert.NotNull(extractedUserId);
        Assert.Equal(userId, extractedUserId);
    }

    [Fact]
    public void GetUserIdFromToken_InvalidToken_ReturnsNull()
    {
        // Act
        var userId = _tokenService.GetUserIdFromToken("invalid.token");

        // Assert
        Assert.Null(userId);
    }

    [Fact]
    public void GetUserIdFromToken_NullToken_ReturnsNull()
    {
        // Act
        var userId = _tokenService.GetUserIdFromToken(null!);

        // Assert
        Assert.Null(userId);
    }

    [Fact]
    public void GenerateAccessToken_TokenHasCorrectIssuerAndAudience()
    {
        // Arrange
        var userId = "user123";
        var (token, _) = _tokenService.GenerateAccessToken(userId, "testuser", "test@example.com", "Coordinator", "Test", "User");

        // Act
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
        Assert.Contains(_jwtSettings.Audience, jwtToken.Audiences);
    }

    [Fact]
    public void GenerateAccessToken_MultipleCallsGenerateUniqueTokens()
    {
        // Arrange
        var userId = "user123";

        // Act
        var (token1, _) = _tokenService.GenerateAccessToken(userId, "testuser", "test@example.com", "Coordinator", "Test", "User");
        var (token2, _) = _tokenService.GenerateAccessToken(userId, "testuser", "test@example.com", "Coordinator", "Test", "User");

        // Assert
        Assert.NotEqual(token1, token2); // JTI claim ensures uniqueness
    }
}
