using BlockchainAidTracker.Blockchain.Configuration;
using BlockchainAidTracker.Blockchain.Persistence;
using BlockchainAidTracker.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BlockchainAidTracker.Tests.Blockchain;

public class JsonBlockchainPersistenceTests : IDisposable
{
    private readonly Mock<ILogger<JsonBlockchainPersistence>> _loggerMock;
    private readonly string _testFilePath;
    private readonly BlockchainPersistenceSettings _settings;

    public JsonBlockchainPersistenceTests()
    {
        _loggerMock = new Mock<ILogger<JsonBlockchainPersistence>>();
        _testFilePath = Path.Combine(Path.GetTempPath(), $"blockchain-test-{Guid.NewGuid()}.json");

        _settings = new BlockchainPersistenceSettings
        {
            Enabled = true,
            FilePath = _testFilePath,
            AutoSaveAfterBlockCreation = true,
            AutoLoadOnStartup = true,
            CreateBackup = false, // Disable backups for simpler tests
            MaxBackupFiles = 0
        };
    }

    public void Dispose()
    {
        // Clean up test file
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }

        // Clean up any backup files
        var directory = Path.GetDirectoryName(_testFilePath);
        var fileName = Path.GetFileName(_testFilePath);
        if (directory != null)
        {
            var backupFiles = Directory.GetFiles(directory, $"{fileName}.*.bak");
            foreach (var backup in backupFiles)
            {
                File.Delete(backup);
            }
        }
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateFileWithBlockchainData()
    {
        // Arrange
        var persistence = new JsonBlockchainPersistence(_settings, _loggerMock.Object);

        var chain = new List<Block>
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
            }
        };

        var pendingTransactions = new List<Transaction>
        {
            new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                Type = TransactionType.ShipmentCreated,
                Timestamp = DateTime.UtcNow,
                SenderPublicKey = "test-key",
                PayloadData = "test-payload",
                Signature = "test-signature"
            }
        };

        // Act
        await persistence.SaveAsync(chain, pendingTransactions);

        // Assert
        Assert.True(File.Exists(_testFilePath));

        var content = await File.ReadAllTextAsync(_testFilePath);
        Assert.Contains("genesis-hash", content);
        Assert.Contains("test-payload", content);
    }

    [Fact]
    public async Task LoadAsync_ShouldReturnNullWhenFileDoesNotExist()
    {
        // Arrange
        var persistence = new JsonBlockchainPersistence(_settings, _loggerMock.Object);

        // Act
        var result = await persistence.LoadAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadAsync_ShouldLoadSavedBlockchainData()
    {
        // Arrange
        var persistence = new JsonBlockchainPersistence(_settings, _loggerMock.Object);

        var originalChain = new List<Block>
        {
            new Block
            {
                Index = 0,
                Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Hash = "block-0-hash",
                PreviousHash = "0",
                Transactions = new List<Transaction>(),
                ValidatorPublicKey = "GENESIS",
                ValidatorSignature = string.Empty
            },
            new Block
            {
                Index = 1,
                Timestamp = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                Hash = "block-1-hash",
                PreviousHash = "block-0-hash",
                Transactions = new List<Transaction>
                {
                    new Transaction
                    {
                        Id = "tx-1",
                        Type = TransactionType.ShipmentCreated,
                        Timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                        SenderPublicKey = "sender-key",
                        PayloadData = "shipment-data",
                        Signature = "tx-signature"
                    }
                },
                ValidatorPublicKey = "validator-key",
                ValidatorSignature = "block-signature"
            }
        };

        var originalPending = new List<Transaction>
        {
            new Transaction
            {
                Id = "pending-tx-1",
                Type = TransactionType.StatusUpdated,
                Timestamp = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                SenderPublicKey = "pending-sender",
                PayloadData = "pending-data",
                Signature = "pending-signature"
            }
        };

        await persistence.SaveAsync(originalChain, originalPending);

        // Act
        var result = await persistence.LoadAsync();

        // Assert
        Assert.NotNull(result);
        var (loadedChain, loadedPending) = result.Value;

        Assert.Equal(2, loadedChain.Count);
        Assert.Single(loadedPending);

        // Verify genesis block
        Assert.Equal(0, loadedChain[0].Index);
        Assert.Equal("block-0-hash", loadedChain[0].Hash);
        Assert.Equal("0", loadedChain[0].PreviousHash);
        Assert.Equal("GENESIS", loadedChain[0].ValidatorPublicKey);

        // Verify second block
        Assert.Equal(1, loadedChain[1].Index);
        Assert.Equal("block-1-hash", loadedChain[1].Hash);
        Assert.Equal("block-0-hash", loadedChain[1].PreviousHash);
        Assert.Single(loadedChain[1].Transactions);
        Assert.Equal("tx-1", loadedChain[1].Transactions[0].Id);

        // Verify pending transaction
        Assert.Equal("pending-tx-1", loadedPending[0].Id);
        Assert.Equal(TransactionType.StatusUpdated, loadedPending[0].Type);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalseWhenFileDoesNotExist()
    {
        // Arrange
        var persistence = new JsonBlockchainPersistence(_settings, _loggerMock.Object);

        // Act
        var exists = await persistence.ExistsAsync();

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueAfterSaving()
    {
        // Arrange
        var persistence = new JsonBlockchainPersistence(_settings, _loggerMock.Object);
        var chain = new List<Block> { CreateGenesisBlock() };
        var pending = new List<Transaction>();

        // Act
        await persistence.SaveAsync(chain, pending);
        var exists = await persistence.ExistsAsync();

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFile()
    {
        // Arrange
        var persistence = new JsonBlockchainPersistence(_settings, _loggerMock.Object);
        var chain = new List<Block> { CreateGenesisBlock() };
        var pending = new List<Transaction>();

        await persistence.SaveAsync(chain, pending);
        Assert.True(File.Exists(_testFilePath));

        // Act
        await persistence.DeleteAsync();

        // Assert
        Assert.False(File.Exists(_testFilePath));
    }

    [Fact]
    public async Task SaveAsync_WithBackupEnabled_ShouldCreateBackupFile()
    {
        // Arrange
        _settings.CreateBackup = true;
        _settings.MaxBackupFiles = 3;

        var persistence = new JsonBlockchainPersistence(_settings, _loggerMock.Object);
        var chain = new List<Block> { CreateGenesisBlock() };
        var pending = new List<Transaction>();

        // Save first time (no backup needed)
        await persistence.SaveAsync(chain, pending);

        // Modify and save second time (backup should be created)
        chain.Add(new Block
        {
            Index = 1,
            Timestamp = DateTime.UtcNow,
            Hash = "block-1",
            PreviousHash = "genesis-hash",
            Transactions = new List<Transaction>(),
            ValidatorPublicKey = "validator",
            ValidatorSignature = "signature"
        });

        // Act
        await persistence.SaveAsync(chain, pending);

        // Assert
        var directory = Path.GetDirectoryName(_testFilePath);
        var fileName = Path.GetFileName(_testFilePath);
        var backupFiles = Directory.GetFiles(directory!, $"{fileName}.*.bak");

        Assert.NotEmpty(backupFiles);
    }

    [Fact]
    public async Task SaveAsync_WithBackupRotation_ShouldKeepOnlyMaxBackupFiles()
    {
        // Arrange
        _settings.CreateBackup = true;
        _settings.MaxBackupFiles = 2;

        var persistence = new JsonBlockchainPersistence(_settings, _loggerMock.Object);
        var chain = new List<Block> { CreateGenesisBlock() };
        var pending = new List<Transaction>();

        // Save multiple times to create several backups
        for (int i = 0; i < 5; i++)
        {
            await persistence.SaveAsync(chain, pending);
            await Task.Delay(100); // Ensure different timestamps
        }

        // Assert
        var directory = Path.GetDirectoryName(_testFilePath);
        var fileName = Path.GetFileName(_testFilePath);
        var backupFiles = Directory.GetFiles(directory!, $"{fileName}.*.bak");

        Assert.True(backupFiles.Length <= _settings.MaxBackupFiles,
            $"Expected at most {_settings.MaxBackupFiles} backup files, but found {backupFiles.Length}");
    }

    [Fact]
    public async Task SaveAsync_ShouldThrowArgumentNullException_WhenChainIsNull()
    {
        // Arrange
        var persistence = new JsonBlockchainPersistence(_settings, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            persistence.SaveAsync(null!, new List<Transaction>()));
    }

    [Fact]
    public async Task SaveAsync_ShouldThrowArgumentNullException_WhenPendingTransactionsIsNull()
    {
        // Arrange
        var persistence = new JsonBlockchainPersistence(_settings, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            persistence.SaveAsync(new List<Block>(), null!));
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateDirectoryIfNotExists()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        var filePath = Path.Combine(nonExistentDir, "blockchain.json");

        var settings = new BlockchainPersistenceSettings
        {
            Enabled = true,
            FilePath = filePath,
            CreateBackup = false
        };

        var persistence = new JsonBlockchainPersistence(settings, _loggerMock.Object);
        var chain = new List<Block> { CreateGenesisBlock() };
        var pending = new List<Transaction>();

        try
        {
            // Act
            await persistence.SaveAsync(chain, pending);

            // Assert
            Assert.True(Directory.Exists(nonExistentDir));
            Assert.True(File.Exists(filePath));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(nonExistentDir))
            {
                Directory.Delete(nonExistentDir, recursive: true);
            }
        }
    }

    private Block CreateGenesisBlock()
    {
        return new Block
        {
            Index = 0,
            Timestamp = DateTime.UtcNow,
            Hash = "genesis-hash",
            PreviousHash = "0",
            Transactions = new List<Transaction>(),
            ValidatorPublicKey = "GENESIS",
            ValidatorSignature = string.Empty
        };
    }
}
