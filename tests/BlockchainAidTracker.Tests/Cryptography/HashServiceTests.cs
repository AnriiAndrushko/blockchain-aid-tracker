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
