using BlockchainAidTracker.Core.Extensions;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Cryptography;
using FluentAssertions;

namespace BlockchainAidTracker.Tests.Core;

public class TransactionTests
{
    private readonly DigitalSignatureService _signatureService;

    public TransactionTests()
    {
        _signatureService = new DigitalSignatureService();
    }

    [Fact]
    public void Constructor_WithNoParameters_CreatesTransactionWithId()
    {
        // Act
        var transaction = new Transaction();

        // Assert
        transaction.Id.Should().NotBeNullOrEmpty();
        transaction.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithParameters_CreatesTransactionCorrectly()
    {
        // Arrange
        var type = TransactionType.ShipmentCreated;
        var senderPublicKey = "TestPublicKey";
        var payloadData = "Test payload";

        // Act
        var transaction = new Transaction(type, senderPublicKey, payloadData);

        // Assert
        transaction.Id.Should().NotBeNullOrEmpty();
        transaction.Type.Should().Be(type);
        transaction.SenderPublicKey.Should().Be(senderPublicKey);
        transaction.PayloadData.Should().Be(payloadData);
        transaction.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetSignatureData_ReturnsConsistentString()
    {
        // Arrange
        var transaction = new Transaction(
            TransactionType.ShipmentCreated,
            "PublicKey123",
            "Payload123"
        );

        // Act
        var signatureData1 = transaction.GetSignatureData();
        var signatureData2 = transaction.GetSignatureData();

        // Assert
        signatureData1.Should().Be(signatureData2);
        signatureData1.Should().Contain(transaction.Id);
        signatureData1.Should().Contain(transaction.SenderPublicKey);
        signatureData1.Should().Contain(transaction.PayloadData);
    }

    [Fact]
    public void Sign_WithValidPrivateKey_SetsSignature()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var transaction = new Transaction(
            TransactionType.ShipmentCreated,
            publicKey,
            "Test payload"
        );

        // Act
        transaction.Sign(privateKey, _signatureService);

        // Assert
        transaction.Signature.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Sign_WithNullPrivateKey_ThrowsArgumentException()
    {
        // Arrange
        var transaction = new Transaction();

        // Act
        var act = () => transaction.Sign(null!, _signatureService);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Private key cannot be null or empty.*");
    }

    [Fact]
    public void Sign_WithNullSignatureService_ThrowsArgumentNullException()
    {
        // Arrange
        var transaction = new Transaction();

        // Act
        var act = () => transaction.Sign("privateKey", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VerifySignature_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var transaction = new Transaction(
            TransactionType.ShipmentCreated,
            publicKey,
            "Test payload"
        );
        transaction.Sign(privateKey, _signatureService);

        // Act
        var result = transaction.VerifySignature(_signatureService);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_WithModifiedPayload_ReturnsFalse()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var transaction = new Transaction(
            TransactionType.ShipmentCreated,
            publicKey,
            "Original payload"
        );
        transaction.Sign(privateKey, _signatureService);

        // Modify the payload after signing (tampering)
        transaction.PayloadData = "Modified payload";

        // Act
        var result = transaction.VerifySignature(_signatureService);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_WithoutSignature_ReturnsFalse()
    {
        // Arrange
        var transaction = new Transaction(
            TransactionType.ShipmentCreated,
            "PublicKey",
            "Payload"
        );

        // Act
        var result = transaction.VerifySignature(_signatureService);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_WithNullSignatureService_ThrowsArgumentNullException()
    {
        // Arrange
        var transaction = new Transaction();

        // Act
        var act = () => transaction.VerifySignature(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VerifySignature_WithEmptySenderPublicKey_ReturnsFalse()
    {
        // Arrange
        var (_, privateKey) = _signatureService.GenerateKeyPair();
        var transaction = new Transaction
        {
            SenderPublicKey = "",
            PayloadData = "Test"
        };
        transaction.Signature = "SomeSignature";

        // Act
        var result = transaction.VerifySignature(_signatureService);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TransactionType_CanBeSet()
    {
        // Arrange
        var transaction = new Transaction();

        // Act
        transaction.Type = TransactionType.DeliveryConfirmed;

        // Assert
        transaction.Type.Should().Be(TransactionType.DeliveryConfirmed);
    }

    [Fact]
    public void EndToEnd_CreateSignAndVerify_WorksCorrectly()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();

        // Act - Create transaction
        var transaction = new Transaction(
            TransactionType.ShipmentCreated,
            publicKey,
            "{\"shipmentId\": \"SHP-001\", \"items\": \"Medical Supplies\"}"
        );

        // Act - Sign transaction
        transaction.Sign(privateKey, _signatureService);

        // Act - Verify transaction
        var isValid = transaction.VerifySignature(_signatureService);

        // Assert
        transaction.Id.Should().NotBeNullOrEmpty();
        transaction.Signature.Should().NotBeNullOrEmpty();
        isValid.Should().BeTrue();
    }
}
