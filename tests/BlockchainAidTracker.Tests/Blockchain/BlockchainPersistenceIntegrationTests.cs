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
            ValidateTransactionSignatures = true,  // Enable validation
            ValidateBlockSignatures = true         // Enable validation
        };

        // Generate key pair - NOTE: GenerateKeyPair returns (PublicKey, PrivateKey)
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();

        // Add a transaction with proper signature
        var transaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = publicKey,
            PayloadData = "Test shipment data"
        };
        transaction.Sign(privateKey, _signatureService);

        blockchain.AddTransaction(transaction);

        // Create a block and sign it (use actual public key, not a string!)
        var block = blockchain.CreateBlock(publicKey);  // Use the actual public key
        block.SignBlock(privateKey, _signatureService);
        var blockAdded = blockchain.AddBlock(block);
        Assert.True(blockAdded, "Block should be added successfully");

        // Save to persistence
        await blockchain.SaveToPersistenceAsync();

        // Assert file was created
        Assert.True(File.Exists(_testFilePath));

        // Act - Load into a new blockchain
        var newBlockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService, persistence)
        {
            ValidateTransactionSignatures = true,  // Enable validation
            ValidateBlockSignatures = true         // Enable validation
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
            ValidateTransactionSignatures = true,  // Enable validation
            ValidateBlockSignatures = true         // Enable validation
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
            ValidateTransactionSignatures = true,  // Enable validation
            ValidateBlockSignatures = true         // Enable validation
        };

        // Generate key pairs for pending transactions
        var (publicKey1, privateKey1) = _signatureService.GenerateKeyPair();
        var (publicKey2, privateKey2) = _signatureService.GenerateKeyPair();

        // Add pending transactions with proper signatures
        var pendingTx1 = new Transaction
        {
            Id = "pending-1",
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = publicKey1,
            PayloadData = "pending-data-1"
        };
        pendingTx1.Sign(privateKey1, _signatureService);

        var pendingTx2 = new Transaction
        {
            Id = "pending-2",
            Type = TransactionType.StatusUpdated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = publicKey2,
            PayloadData = "pending-data-2"
        };
        pendingTx2.Sign(privateKey2, _signatureService);

        blockchain.AddTransaction(pendingTx1);
        blockchain.AddTransaction(pendingTx2);

        // Save
        await blockchain.SaveToPersistenceAsync();

        // Act - Load into new blockchain
        var newBlockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService, persistence)
        {
            ValidateTransactionSignatures = true,  // Enable validation
            ValidateBlockSignatures = true         // Enable validation
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
            ValidateTransactionSignatures = true,  // Enable validation
            ValidateBlockSignatures = true         // Enable validation
        };

        // Create a valid chain - NOTE: GenerateKeyPair returns (PublicKey, PrivateKey)
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();

        // Add first block
        var tx1 = new Transaction
        {
            Id = "tx-1",
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = publicKey,
            PayloadData = "shipment-1"
        };
        tx1.Sign(privateKey, _signatureService);
        blockchain.AddTransaction(tx1);

        var block1 = blockchain.CreateBlock(publicKey);  // Use actual public key
        block1.SignBlock(privateKey, _signatureService);
        var block1Added = blockchain.AddBlock(block1);
        Assert.True(block1Added, "First block should be added successfully");

        // Add second block
        var tx2 = new Transaction
        {
            Id = "tx-2",
            Type = TransactionType.StatusUpdated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = publicKey,
            PayloadData = "shipment-2"
        };
        tx2.Sign(privateKey, _signatureService);
        blockchain.AddTransaction(tx2);

        var block2 = blockchain.CreateBlock(publicKey);  // Use actual public key
        block2.SignBlock(privateKey, _signatureService);
        var block2Added = blockchain.AddBlock(block2);
        Assert.True(block2Added, "Second block should be added successfully");

        // Save
        await blockchain.SaveToPersistenceAsync();

        // Act - Load and validate
        var newBlockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService, persistence)
        {
            ValidateTransactionSignatures = true,  // Enable validation
            ValidateBlockSignatures = true         // Enable validation
        };

        var loaded = await newBlockchain.LoadFromPersistenceAsync();

        // Assert
        Assert.True(loaded);
        Assert.Equal(3, newBlockchain.Chain.Count); // Genesis + 2 blocks
        Assert.True(newBlockchain.IsValidChain()); // Chain integrity validated
    }
}
