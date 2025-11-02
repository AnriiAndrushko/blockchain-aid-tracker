using BlockchainAidTracker.Cryptography;
using FluentAssertions;

namespace BlockchainAidTracker.Tests.Cryptography;

public class HashServiceTests
{
    private readonly HashService _hashService;

    public HashServiceTests()
    {
        _hashService = new HashService();
    }

    [Fact]
    public void ComputeSha256Hash_WithValidString_ReturnsCorrectHash()
    {
        // Arrange
        var input = "Hello, World!";
        var expectedHash = "DFFD6021BB2BD5B0AF676290809EC3A53191DD81C7F70A4B28688A362182986F";

        // Act
        var result = _hashService.ComputeSha256Hash(input);

        // Assert
        result.Should().Be(expectedHash);
    }

    [Fact]
    public void ComputeSha256Hash_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var input = "";

        // Act
        var act = () => _hashService.ComputeSha256Hash(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Input cannot be null or empty.*");
    }

    [Fact]
    public void ComputeSha256Hash_WithNullString_ThrowsArgumentException()
    {
        // Arrange
        string? input = null;

        // Act
        var act = () => _hashService.ComputeSha256Hash(input!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Input cannot be null or empty.*");
    }

    [Fact]
    public void ComputeSha256Hash_WithSameInput_ReturnsSameHash()
    {
        // Arrange
        var input = "Consistent Input";

        // Act
        var hash1 = _hashService.ComputeSha256Hash(input);
        var hash2 = _hashService.ComputeSha256Hash(input);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeSha256Hash_WithDifferentInputs_ReturnsDifferentHashes()
    {
        // Arrange
        var input1 = "Input 1";
        var input2 = "Input 2";

        // Act
        var hash1 = _hashService.ComputeSha256Hash(input1);
        var hash2 = _hashService.ComputeSha256Hash(input2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeSha256Hash_WithByteArray_ReturnsCorrectHash()
    {
        // Arrange
        var input = System.Text.Encoding.UTF8.GetBytes("Test Data");
        var expectedHash = "BCFE67172A6F4079D69FE2F27A9960F9D62EDAE2FCD4BB5A606C2EBB74B3BA65";

        // Act
        var result = _hashService.ComputeSha256Hash(input);

        // Assert
        result.Should().Be(expectedHash);
    }

    [Fact]
    public void ComputeSha256Hash_WithEmptyByteArray_ThrowsArgumentException()
    {
        // Arrange
        var input = Array.Empty<byte>();

        // Act
        var act = () => _hashService.ComputeSha256Hash(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Input cannot be null or empty.*");
    }

    [Fact]
    public void ComputeSha256Hash_WithNullByteArray_ThrowsArgumentException()
    {
        // Arrange
        byte[]? input = null;

        // Act
        var act = () => _hashService.ComputeSha256Hash(input!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Input cannot be null or empty.*");
    }

    [Fact]
    public void ComputeSha256Hash_ReturnsUppercaseHexString()
    {
        // Arrange
        var input = "Test";

        // Act
        var result = _hashService.ComputeSha256Hash(input);

        // Assert
        result.Should().MatchRegex("^[A-F0-9]+$");
    }

    [Fact]
    public void ComputeSha256Hash_Returns64CharacterString()
    {
        // Arrange
        var input = "Any input";

        // Act
        var result = _hashService.ComputeSha256Hash(input);

        // Assert
        result.Should().HaveLength(64); // SHA-256 produces 32 bytes = 64 hex characters
    }
}
