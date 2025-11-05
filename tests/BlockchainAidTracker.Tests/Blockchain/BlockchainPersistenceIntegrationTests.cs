using BlockchainAidTracker.Blockchain;
using BlockchainAidTracker.Blockchain.Configuration;
using BlockchainAidTracker.Blockchain.Persistence;
using BlockchainAidTracker.Core.Extensions;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Cryptography;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BlockchainAidTracker.Tests.Blockchain;

public class BlockchainPersistenceIntegrationTests : IDisposable
{
    private readonly HashService _hashService;
    private readonly DigitalSignatureService _signatureService;
    private readonly string _testFilePath;
    private readonly Mock<ILogger<JsonBlockchainPersistence>> _loggerMock;

    public BlockchainPersistenceIntegrationTests()
    {
        _hashService = new HashService();
        _signatureService = new DigitalSignatureService();
        _testFilePath = Path.Combine(Path.GetTempPath(), $"blockchain-integration-test-{Guid.NewGuid()}.json");
        _loggerMock = new Mock<ILogger<JsonBlockchainPersistence>>();
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task Blockchain_WithPersistence_CanSaveAndLoad()
    {
        // Arrange
        var settings = new BlockchainPersistenceSettings
        {
            Enabled = true,
            FilePath = _testFilePath,
            CreateBackup = false
        };

        var persistence = new JsonBlockchainPersistence(settings, _loggerMock.Object);
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService, persistence)
        {
            ValidateTransactionSignatures = false,
            ValidateBlockSignatures = false
        };

        // Generate key pair for transaction signing
        var (privateKey, publicKey) = _signatureService.GenerateKeyPair();

        // Add a transaction
        var transaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = publicKey,
            PayloadData = "Test shipment data",
            Signature = "test-signature"
        };

        blockchain.AddTransaction(transaction);

        // Create a block
        var block = blockchain.CreateBlock("test-validator");
        block.SignBlock(privateKey, _signatureService);
        blockchain.AddBlock(block);

        // Save to persistence
        await blockchain.SaveToPersistenceAsync();

        // Assert file was created
        Assert.True(File.Exists(_testFilePath));

        // Act - Load into a new blockchain
        var newBlockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService, persistence)
        {
            ValidateTransactionSignatures = false,
            ValidateBlockSignatures = false
        };

        var loaded = await newBlockchain.LoadFromPersistenceAsync();

        // Assert
        Assert.True(loaded);
        Assert.Equal(2, newBlockchain.Chain.Count); // Genesis + 1 block
        Assert.Equal(blockchain.Chain[1].Hash, newBlockchain.Chain[1].Hash);
        Assert.Single(newBlockchain.Chain[1].Transactions);
        Assert.Equal(transaction.Id, newBlockchain.Chain[1].Transactions[0].Id);
    }

    [Fact]
    public async Task LoadFromPersistenceAsync_WithoutPersistence_ReturnsFalse()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Act
        var loaded = await blockchain.LoadFromPersistenceAsync();

        // Assert
        Assert.False(loaded);
    }

    [Fact]
    public async Task SaveToPersistenceAsync_WithoutPersistence_DoesNotThrow()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Act & Assert (should not throw)
        await blockchain.SaveToPersistenceAsync();
    }

    [Fact]
    public async Task LoadFromPersistenceAsync_WithInvalidData_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new BlockchainPersistenceSettings
        {
            Enabled = true,
            FilePath = _testFilePath,
            CreateBackup = false
        };

        // Create invalid blockchain data (broken chain)
        var invalidChain = new List<Block>
        {
            new Block
            {
                Index = 0,
                Timestamp = DateTime.UtcNow,
                Hash = "genesis-hash",
                PreviousHash = "0",
                Transactions = new List<Transaction>(),
                ValidatorPublicKey = "GENESIS",
                ValidatorSignature = string.Empty
            },
            new Block
            {
                Index = 2, // Invalid: should be 1
                Timestamp = DateTime.UtcNow,
                Hash = "wrong-hash",
                PreviousHash = "wrong-previous",
                Transactions = new List<Transaction>(),
                ValidatorPublicKey = "validator",
                ValidatorSignature = string.Empty
            }
        };

        var persistence = new JsonBlockchainPersistence(settings, _loggerMock.Object);
        await persistence.SaveAsync(invalidChain, new List<Transaction>());

        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService, persistence)
        {
            ValidateTransactionSignatures = false,
            ValidateBlockSignatures = false
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => blockchain.LoadFromPersistenceAsync());
    }

    [Fact]
    public async Task LoadFromPersistenceAsync_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var settings = new BlockchainPersistenceSettings
        {
            Enabled = true,
            FilePath = _testFilePath,
            CreateBackup = false
        };

        var persistence = new JsonBlockchainPersistence(settings, _loggerMock.Object);
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService, persistence);

        // Act
        var loaded = await blockchain.LoadFromPersistenceAsync();

        // Assert
        Assert.False(loaded);
        Assert.Single(blockchain.Chain); // Should still have genesis block
    }

    [Fact]
    public async Task Blockchain_LoadFromPersistence_PreservePendingTransactions()
    {
        // Arrange
        var settings = new BlockchainPersistenceSettings
        {
            Enabled = true,
            FilePath = _testFilePath,
            CreateBackup = false
        };

        var persistence = new JsonBlockchainPersistence(settings, _loggerMock.Object);
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService, persistence)
        {
            ValidateTransactionSignatures = false,
            ValidateBlockSignatures = false
        };

        // Add pending transactions
        var pendingTx1 = new Transaction
        {
            Id = "pending-1",
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = "sender-1",
            PayloadData = "pending-data-1",
            Signature = "signature-1"
        };

        var pendingTx2 = new Transaction
        {
            Id = "pending-2",
            Type = TransactionType.StatusUpdated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = "sender-2",
            PayloadData = "pending-data-2",
            Signature = "signature-2"
        };

        blockchain.AddTransaction(pendingTx1);
        blockchain.AddTransaction(pendingTx2);

        // Save
        await blockchain.SaveToPersistenceAsync();

        // Act - Load into new blockchain
        var newBlockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService, persistence)
        {
            ValidateTransactionSignatures = false,
            ValidateBlockSignatures = false
        };

        var loaded = await newBlockchain.LoadFromPersistenceAsync();

        // Assert
        Assert.True(loaded);
        Assert.Equal(2, newBlockchain.PendingTransactions.Count);
        Assert.Contains(newBlockchain.PendingTransactions, tx => tx.Id == "pending-1");
        Assert.Contains(newBlockchain.PendingTransactions, tx => tx.Id == "pending-2");
    }

    [Fact]
    public async Task Blockchain_LoadFromPersistence_ValidatesChainIntegrity()
    {
        // Arrange
        var settings = new BlockchainPersistenceSettings
        {
            Enabled = true,
            FilePath = _testFilePath,
            CreateBackup = false
        };

        var persistence = new JsonBlockchainPersistence(settings, _loggerMock.Object);
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService, persistence)
        {
            ValidateTransactionSignatures = false,
            ValidateBlockSignatures = false
        };

        // Create a valid chain
        var (privateKey, publicKey) = _signatureService.GenerateKeyPair();

        // Add first block
        var tx1 = new Transaction
        {
            Id = "tx-1",
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = publicKey,
            PayloadData = "shipment-1",
            Signature = "sig-1"
        };
        blockchain.AddTransaction(tx1);

        var block1 = blockchain.CreateBlock("validator-1");
        block1.SignBlock(privateKey, _signatureService);
        blockchain.AddBlock(block1);

        // Add second block
        var tx2 = new Transaction
        {
            Id = "tx-2",
            Type = TransactionType.StatusUpdated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = publicKey,
            PayloadData = "shipment-2",
            Signature = "sig-2"
        };
        blockchain.AddTransaction(tx2);

        var block2 = blockchain.CreateBlock("validator-2");
        block2.SignBlock(privateKey, _signatureService);
        blockchain.AddBlock(block2);

        // Save
        await blockchain.SaveToPersistenceAsync();

        // Act - Load and validate
        var newBlockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService, persistence)
        {
            ValidateTransactionSignatures = false,
            ValidateBlockSignatures = false
        };

        var loaded = await newBlockchain.LoadFromPersistenceAsync();

        // Assert
        Assert.True(loaded);
        Assert.Equal(3, newBlockchain.Chain.Count); // Genesis + 2 blocks
        Assert.True(newBlockchain.IsValidChain()); // Chain integrity validated
    }
}
