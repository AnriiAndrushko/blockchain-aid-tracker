using BlockchainAidTracker.Blockchain;
using BlockchainAidTracker.Core.Extensions;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Cryptography;
using FluentAssertions;

namespace BlockchainAidTracker.Tests.Blockchain;

public class BlockchainTests
{
    private readonly HashService _hashService;
    private readonly DigitalSignatureService _signatureService;

    public BlockchainTests()
    {
        _hashService = new HashService();
        _signatureService = new DigitalSignatureService();
    }

    [Fact]
    public void Constructor_InitializesWithGenesisBlock()
    {
        // Act
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Assert
        blockchain.Chain.Should().HaveCount(1);
        blockchain.Chain[0].Index.Should().Be(0);
        blockchain.Chain[0].PreviousHash.Should().Be("0");
        blockchain.Chain[0].ValidatorPublicKey.Should().Be("GENESIS");
    }

    [Fact]
    public void Constructor_WithNullHashService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new BlockchainAidTracker.Blockchain.Blockchain(null!, _signatureService);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullSignatureService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new BlockchainAidTracker.Blockchain.Blockchain(_hashService, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetLatestBlock_ReturnsLastBlock()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Act
        var latestBlock = blockchain.GetLatestBlock();

        // Assert
        latestBlock.Should().NotBeNull();
        latestBlock.Index.Should().Be(0);
    }

    [Fact]
    public void AddTransaction_WithValidTransaction_AddsToPool()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var transaction = new Transaction(TransactionType.ShipmentCreated, publicKey, "Test payload");
        transaction.Sign(privateKey, _signatureService);

        // Act
        blockchain.AddTransaction(transaction);

        // Assert
        blockchain.PendingTransactions.Should().HaveCount(1);
        blockchain.PendingTransactions[0].Should().Be(transaction);
    }

    [Fact]
    public void AddTransaction_WithoutSenderPublicKey_ThrowsArgumentException()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var transaction = new Transaction
        {
            SenderPublicKey = "",
            PayloadData = "Test"
        };

        // Act
        var act = () => blockchain.AddTransaction(transaction);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Transaction must have a sender public key.");
    }

    [Fact]
    public void AddTransaction_WithoutPayloadData_ThrowsArgumentException()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var transaction = new Transaction
        {
            SenderPublicKey = "Key",
            PayloadData = ""
        };

        // Act
        var act = () => blockchain.AddTransaction(transaction);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Transaction must have payload data.");
    }

    [Fact]
    public void AddTransaction_WithInvalidSignature_ThrowsInvalidOperationException()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var transaction = new Transaction(TransactionType.ShipmentCreated, "PublicKey", "Payload")
        {
            Signature = "InvalidSignature"
        };

        // Act
        var act = () => blockchain.AddTransaction(transaction);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Transaction signature is invalid.");
    }

    [Fact]
    public void AddTransaction_WithValidationDisabled_AcceptsUnsignedTransaction()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService)
        {
            ValidateTransactionSignatures = false
        };
        var transaction = new Transaction(TransactionType.ShipmentCreated, "PublicKey", "Payload");

        // Act
        blockchain.AddTransaction(transaction);

        // Assert
        blockchain.PendingTransactions.Should().HaveCount(1);
    }

    [Fact]
    public void CreateBlock_WithPendingTransactions_CreatesBlock()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var transaction = new Transaction(TransactionType.ShipmentCreated, publicKey, "Test");
        transaction.Sign(privateKey, _signatureService);
        blockchain.AddTransaction(transaction);

        var (validatorPublicKey, _) = _signatureService.GenerateKeyPair();

        // Act
        var newBlock = blockchain.CreateBlock(validatorPublicKey);

        // Assert
        newBlock.Should().NotBeNull();
        newBlock.Index.Should().Be(1);
        newBlock.Transactions.Should().HaveCount(1);
        newBlock.PreviousHash.Should().Be(blockchain.Chain[0].Hash);
        newBlock.ValidatorPublicKey.Should().Be(validatorPublicKey);
        newBlock.Hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateBlock_WithNoPendingTransactions_ThrowsInvalidOperationException()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Act
        var act = () => blockchain.CreateBlock("ValidatorKey");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No pending transactions to create a block.");
    }

    [Fact]
    public void AddBlock_WithValidBlock_AddsToChain()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var (senderPublicKey, senderPrivateKey) = _signatureService.GenerateKeyPair();
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();

        var transaction = new Transaction(TransactionType.ShipmentCreated, senderPublicKey, "Test");
        transaction.Sign(senderPrivateKey, _signatureService);
        blockchain.AddTransaction(transaction);

        var newBlock = blockchain.CreateBlock(validatorPublicKey);
        newBlock.SignBlock(validatorPrivateKey, _signatureService);

        // Act
        var result = blockchain.AddBlock(newBlock);

        // Assert
        result.Should().BeTrue();
        blockchain.Chain.Should().HaveCount(2);
        blockchain.PendingTransactions.Should().BeEmpty();
    }

    [Fact]
    public void AddBlock_WithInvalidIndex_ReturnsFalse()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();

        var invalidBlock = new Block
        {
            Index = 5, // Wrong index
            PreviousHash = blockchain.GetLatestBlock().Hash,
            ValidatorPublicKey = validatorPublicKey
        };
        invalidBlock.Hash = _hashService.ComputeSha256Hash(invalidBlock.CalculateHashData());
        invalidBlock.SignBlock(validatorPrivateKey, _signatureService);

        // Act
        var result = blockchain.AddBlock(invalidBlock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AddBlock_WithInvalidPreviousHash_ReturnsFalse()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();

        var invalidBlock = new Block
        {
            Index = 1,
            PreviousHash = "WrongHash",
            ValidatorPublicKey = validatorPublicKey
        };
        invalidBlock.Hash = _hashService.ComputeSha256Hash(invalidBlock.CalculateHashData());
        invalidBlock.SignBlock(validatorPrivateKey, _signatureService);

        // Act
        var result = blockchain.AddBlock(invalidBlock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AddBlock_WithInvalidHash_ReturnsFalse()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();

        var invalidBlock = new Block
        {
            Index = 1,
            PreviousHash = blockchain.GetLatestBlock().Hash,
            Hash = "InvalidHash",
            ValidatorPublicKey = validatorPublicKey
        };
        invalidBlock.SignBlock(validatorPrivateKey, _signatureService);

        // Act
        var result = blockchain.AddBlock(invalidBlock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidChain_WithValidChain_ReturnsTrue()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var (senderPublicKey, senderPrivateKey) = _signatureService.GenerateKeyPair();
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();

        var transaction = new Transaction(TransactionType.ShipmentCreated, senderPublicKey, "Test");
        transaction.Sign(senderPrivateKey, _signatureService);
        blockchain.AddTransaction(transaction);

        var newBlock = blockchain.CreateBlock(validatorPublicKey);
        newBlock.SignBlock(validatorPrivateKey, _signatureService);
        blockchain.AddBlock(newBlock);

        // Act
        var result = blockchain.IsValidChain();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidChain_WithTamperedBlock_ReturnsFalse()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var (senderPublicKey, senderPrivateKey) = _signatureService.GenerateKeyPair();
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();

        var transaction = new Transaction(TransactionType.ShipmentCreated, senderPublicKey, "Test");
        transaction.Sign(senderPrivateKey, _signatureService);
        blockchain.AddTransaction(transaction);

        var newBlock = blockchain.CreateBlock(validatorPublicKey);
        newBlock.SignBlock(validatorPrivateKey, _signatureService);
        blockchain.AddBlock(newBlock);

        // Tamper with a block
        blockchain.Chain[1].Transactions[0].PayloadData = "Tampered";

        // Act
        var result = blockchain.IsValidChain();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetBlockByIndex_WithValidIndex_ReturnsBlock()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Act
        var block = blockchain.GetBlockByIndex(0);

        // Assert
        block.Should().NotBeNull();
        block!.Index.Should().Be(0);
    }

    [Fact]
    public void GetBlockByIndex_WithInvalidIndex_ReturnsNull()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Act
        var block = blockchain.GetBlockByIndex(10);

        // Assert
        block.Should().BeNull();
    }

    [Fact]
    public void GetTransactionById_WithExistingId_ReturnsTransaction()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var (senderPublicKey, senderPrivateKey) = _signatureService.GenerateKeyPair();
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();

        var transaction = new Transaction(TransactionType.ShipmentCreated, senderPublicKey, "Test");
        transaction.Sign(senderPrivateKey, _signatureService);
        blockchain.AddTransaction(transaction);

        var newBlock = blockchain.CreateBlock(validatorPublicKey);
        newBlock.SignBlock(validatorPrivateKey, _signatureService);
        blockchain.AddBlock(newBlock);

        // Act
        var foundTransaction = blockchain.GetTransactionById(transaction.Id);

        // Assert
        foundTransaction.Should().NotBeNull();
        foundTransaction!.Id.Should().Be(transaction.Id);
    }

    [Fact]
    public void GetTransactionById_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Act
        var transaction = blockchain.GetTransactionById("NonExistingId");

        // Assert
        transaction.Should().BeNull();
    }

    [Fact]
    public void GetChainLength_ReturnsCorrectCount()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Act
        var length = blockchain.GetChainLength();

        // Assert
        length.Should().Be(1); // Genesis block
    }

    [Fact]
    public void GetTransactionsBySender_ReturnsCorrectTransactions()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var (sender1PublicKey, sender1PrivateKey) = _signatureService.GenerateKeyPair();
        var (sender2PublicKey, sender2PrivateKey) = _signatureService.GenerateKeyPair();
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();

        var transaction1 = new Transaction(TransactionType.ShipmentCreated, sender1PublicKey, "Test1");
        transaction1.Sign(sender1PrivateKey, _signatureService);
        blockchain.AddTransaction(transaction1);

        var transaction2 = new Transaction(TransactionType.StatusUpdated, sender2PublicKey, "Test2");
        transaction2.Sign(sender2PrivateKey, _signatureService);
        blockchain.AddTransaction(transaction2);

        var newBlock = blockchain.CreateBlock(validatorPublicKey);
        newBlock.SignBlock(validatorPrivateKey, _signatureService);
        blockchain.AddBlock(newBlock);

        // Act
        var sender1Transactions = blockchain.GetTransactionsBySender(sender1PublicKey);

        // Assert
        sender1Transactions.Should().HaveCount(1);
        sender1Transactions[0].SenderPublicKey.Should().Be(sender1PublicKey);
    }

    [Fact]
    public void EndToEnd_CompleteBlockchainWorkflow_WorksCorrectly()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        var (coordinatorPublicKey, coordinatorPrivateKey) = _signatureService.GenerateKeyPair();
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();

        // Create multiple transactions
        for (int i = 0; i < 3; i++)
        {
            var transaction = new Transaction(
                TransactionType.ShipmentCreated,
                coordinatorPublicKey,
                $"{{\"shipmentId\": \"SHP-00{i}\"}}"
            );
            transaction.Sign(coordinatorPrivateKey, _signatureService);
            blockchain.AddTransaction(transaction);
        }

        // Create and add block
        var newBlock = blockchain.CreateBlock(validatorPublicKey);
        newBlock.SignBlock(validatorPrivateKey, _signatureService);
        var added = blockchain.AddBlock(newBlock);

        // Act & Assert
        added.Should().BeTrue();
        blockchain.GetChainLength().Should().Be(2);
        blockchain.PendingTransactions.Should().BeEmpty();
        blockchain.IsValidChain().Should().BeTrue();
        blockchain.Chain[1].Transactions.Should().HaveCount(3);
    }
}
