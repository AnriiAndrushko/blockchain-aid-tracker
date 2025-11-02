using BlockchainAidTracker.Services.Services;
using Xunit;

namespace BlockchainAidTracker.Tests.Services;

/// <summary>
/// Unit tests for PasswordService
/// </summary>
public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashedPassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hashedPassword = _passwordService.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.NotEmpty(hashedPassword);
        Assert.NotEqual(password, hashedPassword);
        Assert.StartsWith("$2", hashedPassword); // BCrypt hash starts with $2
    }

    [Fact]
    public void HashPassword_SamePassword_GeneratesDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // BCrypt uses salt, so hashes should differ
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HashPassword_InvalidPassword_ThrowsArgumentException(string? password)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(password!));
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hashedPassword);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hashedPassword = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_NullPassword_ThrowsArgumentException()
    {
        // Arrange
        var hashedPassword = _passwordService.HashPassword("TestPassword123!");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.VerifyPassword(null!, hashedPassword));
    }

    [Fact]
    public void VerifyPassword_NullHashedPassword_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.VerifyPassword("password", null!));
    }

    [Fact]
    public void VerifyPassword_InvalidHashFormat_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var invalidHash = "not-a-valid-bcrypt-hash";

        // Act
        var result = _passwordService.VerifyPassword(password, invalidHash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_EmptyPassword_ThrowsArgumentException()
    {
        // Arrange
        var hashedPassword = _passwordService.HashPassword("TestPassword123!");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.VerifyPassword("", hashedPassword));
    }

    [Fact]
    public void HashPassword_LongPassword_SuccessfullyHashes()
    {
        // Arrange
        var longPassword = new string('a', 100);

        // Act
        var hashedPassword = _passwordService.HashPassword(longPassword);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.True(_passwordService.VerifyPassword(longPassword, hashedPassword));
    }

    [Fact]
    public void HashPassword_SpecialCharacters_SuccessfullyHashes()
    {
        // Arrange
        var specialPassword = "P@ssw0rd!#$%^&*()_+-=[]{}|;:',.<>?/~`";

        // Act
        var hashedPassword = _passwordService.HashPassword(specialPassword);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.True(_passwordService.VerifyPassword(specialPassword, hashedPassword));
    }

    [Fact]
    public void HashPassword_UnicodeCharacters_SuccessfullyHashes()
    {
        // Arrange
        var unicodePassword = "Пароль123!密码";

        // Act
        var hashedPassword = _passwordService.HashPassword(unicodePassword);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.True(_passwordService.VerifyPassword(unicodePassword, hashedPassword));
    }
}
