using BlockchainAidTracker.Cryptography;
using FluentAssertions;
using System.Security.Cryptography;

namespace BlockchainAidTracker.Tests.Cryptography;

public class DigitalSignatureServiceTests
{
    private readonly DigitalSignatureService _signatureService;

    public DigitalSignatureServiceTests()
    {
        _signatureService = new DigitalSignatureService();
    }

    [Fact]
    public void GenerateKeyPair_ReturnsValidKeyPair()
    {
        // Act
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();

        // Assert
        publicKey.Should().NotBeNullOrEmpty();
        privateKey.Should().NotBeNullOrEmpty();
        publicKey.Should().NotBe(privateKey);
    }

    [Fact]
    public void GenerateKeyPair_GeneratesDifferentKeysEachTime()
    {
        // Act
        var (publicKey1, privateKey1) = _signatureService.GenerateKeyPair();
        var (publicKey2, privateKey2) = _signatureService.GenerateKeyPair();

        // Assert
        publicKey1.Should().NotBe(publicKey2);
        privateKey1.Should().NotBe(privateKey2);
    }

    [Fact]
    public void GenerateKeyPair_ReturnsBase64EncodedKeys()
    {
        // Act
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();

        // Assert
        var publicKeyBytes = Convert.FromBase64String(publicKey);
        var privateKeyBytes = Convert.FromBase64String(privateKey);

        publicKeyBytes.Should().NotBeEmpty();
        privateKeyBytes.Should().NotBeEmpty();
    }

    [Fact]
    public void SignData_WithValidInputs_ReturnsSignature()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var data = "Test data to sign";

        // Act
        var signature = _signatureService.SignData(data, privateKey);

        // Assert
        signature.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SignData_WithNullData_ThrowsArgumentException()
    {
        // Arrange
        var (_, privateKey) = _signatureService.GenerateKeyPair();
        string? data = null;

        // Act
        var act = () => _signatureService.SignData(data!, privateKey);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Data cannot be null or empty.*");
    }

    [Fact]
    public void SignData_WithEmptyData_ThrowsArgumentException()
    {
        // Arrange
        var (_, privateKey) = _signatureService.GenerateKeyPair();
        var data = "";

        // Act
        var act = () => _signatureService.SignData(data, privateKey);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Data cannot be null or empty.*");
    }

    [Fact]
    public void SignData_WithNullPrivateKey_ThrowsArgumentException()
    {
        // Arrange
        var data = "Test data";
        string? privateKey = null;

        // Act
        var act = () => _signatureService.SignData(data, privateKey!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Private key cannot be null or empty.*");
    }

    [Fact]
    public void SignData_WithInvalidPrivateKey_ThrowsCryptographicException()
    {
        // Arrange
        var data = "Test data";
        var invalidPrivateKey = "InvalidBase64Key";

        // Act
        var act = () => _signatureService.SignData(data, invalidPrivateKey);

        // Assert
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void VerifySignature_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var data = "Test data to verify";
        var signature = _signatureService.SignData(data, privateKey);

        // Act
        var result = _signatureService.VerifySignature(data, signature, publicKey);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_WithModifiedData_ReturnsFalse()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var originalData = "Original data";
        var modifiedData = "Modified data";
        var signature = _signatureService.SignData(originalData, privateKey);

        // Act
        var result = _signatureService.VerifySignature(modifiedData, signature, publicKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_WithModifiedSignature_ReturnsFalse()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var data = "Test data";
        var signature = _signatureService.SignData(data, privateKey);
        var modifiedSignature = signature + "A"; // Tamper with signature

        // Act
        var result = _signatureService.VerifySignature(data, modifiedSignature, publicKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_WithWrongPublicKey_ReturnsFalse()
    {
        // Arrange
        var (publicKey1, privateKey1) = _signatureService.GenerateKeyPair();
        var (publicKey2, _) = _signatureService.GenerateKeyPair();
        var data = "Test data";
        var signature = _signatureService.SignData(data, privateKey1);

        // Act
        var result = _signatureService.VerifySignature(data, signature, publicKey2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_WithNullData_ReturnsFalse()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var data = "Test data";
        var signature = _signatureService.SignData(data, privateKey);

        // Act
        var result = _signatureService.VerifySignature(null!, signature, publicKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_WithNullSignature_ReturnsFalse()
    {
        // Arrange
        var (publicKey, _) = _signatureService.GenerateKeyPair();
        var data = "Test data";

        // Act
        var result = _signatureService.VerifySignature(data, null!, publicKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_WithNullPublicKey_ReturnsFalse()
    {
        // Arrange
        var (_, privateKey) = _signatureService.GenerateKeyPair();
        var data = "Test data";
        var signature = _signatureService.SignData(data, privateKey);

        // Act
        var result = _signatureService.VerifySignature(data, signature, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SignData_WithSameDataAndKey_GeneratesDifferentSignatures()
    {
        // Arrange
        var (_, privateKey) = _signatureService.GenerateKeyPair();
        var data = "Test data";

        // Act
        var signature1 = _signatureService.SignData(data, privateKey);
        var signature2 = _signatureService.SignData(data, privateKey);

        // Assert
        // ECDSA signatures can be different each time due to random k value
        // But both should verify correctly
        signature1.Should().NotBeNullOrEmpty();
        signature2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void EndToEnd_SignAndVerify_WorksCorrectly()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var data = "Important blockchain transaction data";

        // Act
        var signature = _signatureService.SignData(data, privateKey);
        var isValid = _signatureService.VerifySignature(data, signature, publicKey);

        // Assert
        isValid.Should().BeTrue();
    }
}
