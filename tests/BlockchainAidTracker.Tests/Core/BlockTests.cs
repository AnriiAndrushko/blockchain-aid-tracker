using BlockchainAidTracker.Core.Extensions;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Cryptography;
using FluentAssertions;

namespace BlockchainAidTracker.Tests.Core;

public class BlockTests
{
    private readonly DigitalSignatureService _signatureService;
    private readonly HashService _hashService;

    public BlockTests()
    {
        _signatureService = new DigitalSignatureService();
        _hashService = new HashService();
    }

    [Fact]
    public void Constructor_WithNoParameters_CreatesBlockWithTimestamp()
    {
        // Act
        var block = new Block();

        // Assert
        block.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        block.Transactions.Should().NotBeNull();
        block.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithParameters_CreatesBlockCorrectly()
    {
        // Arrange
        var index = 1;
        var transactions = new List<Transaction>
        {
            new Transaction(TransactionType.ShipmentCreated, "PublicKey", "Payload")
        };
        var previousHash = "PreviousHash123";

        // Act
        var block = new Block(index, transactions, previousHash);

        // Assert
        block.Index.Should().Be(index);
        block.Transactions.Should().HaveCount(1);
        block.PreviousHash.Should().Be(previousHash);
        block.Nonce.Should().Be(0);
        block.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CalculateHashData_ReturnsConsistentString()
    {
        // Arrange
        var block = new Block(1, new List<Transaction>(), "PrevHash")
        {
            ValidatorPublicKey = "ValidatorKey"
        };

        // Act
        var hashData1 = block.CalculateHashData();
        var hashData2 = block.CalculateHashData();

        // Assert
        hashData1.Should().Be(hashData2);
        hashData1.Should().Contain(block.Index.ToString());
        hashData1.Should().Contain(block.PreviousHash);
        hashData1.Should().Contain(block.ValidatorPublicKey);
    }

    [Fact]
    public void CalculateHashData_IncludesTransactionIds()
    {
        // Arrange
        var transaction1 = new Transaction(TransactionType.ShipmentCreated, "Key1", "Data1");
        var transaction2 = new Transaction(TransactionType.StatusUpdated, "Key2", "Data2");
        var block = new Block(1, new List<Transaction> { transaction1, transaction2 }, "PrevHash");

        // Act
        var hashData = block.CalculateHashData();

        // Assert
        hashData.Should().Contain(transaction1.Id);
        hashData.Should().Contain(transaction2.Id);
    }

    [Fact]
    public void GetValidatorSignatureData_ReturnsConsistentString()
    {
        // Arrange
        var block = new Block(1, new List<Transaction>(), "PrevHash")
        {
            Hash = "BlockHash123",
            ValidatorPublicKey = "ValidatorKey"
        };

        // Act
        var signatureData1 = block.GetValidatorSignatureData();
        var signatureData2 = block.GetValidatorSignatureData();

        // Assert
        signatureData1.Should().Be(signatureData2);
        signatureData1.Should().Contain(block.Index.ToString());
        signatureData1.Should().Contain(block.Hash);
        signatureData1.Should().Contain(block.ValidatorPublicKey);
    }

    [Fact]
    public void SignBlock_WithValidPrivateKey_SetsValidatorSignature()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var block = new Block(1, new List<Transaction>(), "PrevHash")
        {
            Hash = "BlockHash",
            ValidatorPublicKey = publicKey
        };

        // Act
        block.SignBlock(privateKey, _signatureService);

        // Assert
        block.ValidatorSignature.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SignBlock_WithNullPrivateKey_ThrowsArgumentException()
    {
        // Arrange
        var block = new Block { Hash = "Hash" };

        // Act
        var act = () => block.SignBlock(null!, _signatureService);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Validator private key cannot be null or empty.*");
    }

    [Fact]
    public void SignBlock_WithNullSignatureService_ThrowsArgumentNullException()
    {
        // Arrange
        var block = new Block { Hash = "Hash" };

        // Act
        var act = () => block.SignBlock("privateKey", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SignBlock_WithoutHash_ThrowsInvalidOperationException()
    {
        // Arrange
        var (_, privateKey) = _signatureService.GenerateKeyPair();
        var block = new Block();

        // Act
        var act = () => block.SignBlock(privateKey, _signatureService);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Block must have a hash before it can be signed.*");
    }

    [Fact]
    public void VerifyValidatorSignature_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var block = new Block(1, new List<Transaction>(), "PrevHash")
        {
            Hash = "BlockHash",
            ValidatorPublicKey = publicKey
        };
        block.SignBlock(privateKey, _signatureService);

        // Act
        var result = block.VerifyValidatorSignature(_signatureService);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyValidatorSignature_WithModifiedHash_ReturnsFalse()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var block = new Block(1, new List<Transaction>(), "PrevHash")
        {
            Hash = "OriginalHash",
            ValidatorPublicKey = publicKey
        };
        block.SignBlock(privateKey, _signatureService);

        // Modify hash after signing (tampering)
        block.Hash = "ModifiedHash";

        // Act
        var result = block.VerifyValidatorSignature(_signatureService);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyValidatorSignature_WithoutSignature_ReturnsFalse()
    {
        // Arrange
        var block = new Block
        {
            Hash = "Hash",
            ValidatorPublicKey = "Key"
        };

        // Act
        var result = block.VerifyValidatorSignature(_signatureService);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyValidatorSignature_ForGenesisBlock_ReturnsTrue()
    {
        // Arrange
        var genesisBlock = new Block
        {
            Index = 0,
            ValidatorPublicKey = "GENESIS",
            Hash = "GenesisHash"
        };

        // Act
        var result = genesisBlock.VerifyValidatorSignature(_signatureService);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyValidatorSignature_WithNullSignatureService_ThrowsArgumentNullException()
    {
        // Arrange
        var block = new Block();

        // Act
        var act = () => block.VerifyValidatorSignature(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EndToEnd_CreateHashSignAndVerify_WorksCorrectly()
    {
        // Arrange
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var (senderPublicKey, senderPrivateKey) = _signatureService.GenerateKeyPair();

        // Create and sign a transaction
        var transaction = new Transaction(
            TransactionType.ShipmentCreated,
            senderPublicKey,
            "Test payload"
        );
        transaction.Sign(senderPrivateKey, _signatureService);

        // Create block
        var block = new Block(1, new List<Transaction> { transaction }, "PreviousHash")
        {
            ValidatorPublicKey = publicKey
        };

        // Calculate hash
        var hashData = block.CalculateHashData();
        block.Hash = _hashService.ComputeSha256Hash(hashData);

        // Sign block
        block.SignBlock(privateKey, _signatureService);

        // Act - Verify
        var isBlockValid = block.VerifyValidatorSignature(_signatureService);
        var isTransactionValid = transaction.VerifySignature(_signatureService);

        // Assert
        block.Hash.Should().NotBeNullOrEmpty();
        block.ValidatorSignature.Should().NotBeNullOrEmpty();
        isBlockValid.Should().BeTrue();
        isTransactionValid.Should().BeTrue();
    }
}
