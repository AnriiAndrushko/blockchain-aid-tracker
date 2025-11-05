using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Cryptography;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.BackgroundServices;
using BlockchainAidTracker.Services.Configuration;
using BlockchainAidTracker.Services.Consensus;
using BlockchainAidTracker.Services.Interfaces;
using BlockchainAidTracker.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace BlockchainAidTracker.Tests.Services.BackgroundServices;

/// <summary>
/// Unit tests for the BlockCreationBackgroundService class.
/// </summary>
public class BlockCreationBackgroundServiceTests : DatabaseTestBase
{
    private readonly Mock<ILogger<BlockCreationBackgroundService>> _mockLogger;
    private readonly IHashService _hashService;
    private readonly IDigitalSignatureService _signatureService;
    private readonly BlockchainAidTracker.Blockchain.Blockchain _blockchain;
    private readonly ConsensusSettings _consensusSettings;

    public BlockCreationBackgroundServiceTests()
    {
        _mockLogger = new Mock<ILogger<BlockCreationBackgroundService>>();
        _hashService = new HashService();
        _signatureService = new DigitalSignatureService();
        _blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService)
        {
            ValidateTransactionSignatures = false
        };
        _consensusSettings = new ConsensusSettings
        {
            BlockCreationIntervalSeconds = 1, // Short interval for testing
            MinimumTransactionsPerBlock = 1,
            MaximumTransactionsPerBlock = 100,
            ValidatorPassword = "TestPassword123!",
            EnableAutomatedBlockCreation = true
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new BlockCreationBackgroundService(
            null!,
            _mockLogger.Object,
            _consensusSettings,
            _blockchain);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>().Object;

        // Act
        var act = () => new BlockCreationBackgroundService(
            serviceProvider,
            null!,
            _consensusSettings,
            _blockchain);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullConsensusSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>().Object;

        // Act
        var act = () => new BlockCreationBackgroundService(
            serviceProvider,
            _mockLogger.Object,
            null!,
            _blockchain);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("consensusSettings");
    }

    [Fact]
    public void Constructor_WithNullBlockchain_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>().Object;

        // Act
        var act = () => new BlockCreationBackgroundService(
            serviceProvider,
            _mockLogger.Object,
            _consensusSettings,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("blockchain");
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNotCreateBlocks()
    {
        // Arrange
        var disabledSettings = new ConsensusSettings
        {
            EnableAutomatedBlockCreation = false
        };

        var serviceProvider = CreateServiceProvider();
        var service = new BlockCreationBackgroundService(
            serviceProvider,
            _mockLogger.Object,
            disabledSettings,
            _blockchain);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(500); // Give it time to check the flag
        await service.StopAsync(cts.Token);

        // Assert
        _blockchain.Chain.Count.Should().Be(1); // Only genesis block
    }

    [Fact]
    public async Task ExecuteAsync_WithNoPendingTransactions_DoesNotCreateBlock()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var service = new BlockCreationBackgroundService(
            serviceProvider,
            _mockLogger.Object,
            _consensusSettings,
            _blockchain);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(500); // Give it time to check
        await service.StopAsync(cts.Token);

        // Assert
        _blockchain.Chain.Count.Should().Be(1); // Only genesis block
        _blockchain.PendingTransactions.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithPendingTransactionsAndActiveValidator_CreatesBlock()
    {
        // Arrange
        var validator = CreateTestValidator();
        await Context.Validators.AddAsync(validator);
        await Context.SaveChangesAsync();

        // Add a pending transaction
        var transaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = "test-public-key",
            PayloadData = "test-payload",
            Signature = "test-signature"
        };
        _blockchain.AddTransaction(transaction);

        var serviceProvider = CreateServiceProvider();
        var service = new BlockCreationBackgroundService(
            serviceProvider,
            _mockLogger.Object,
            _consensusSettings,
            _blockchain);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(2500); // Wait longer: interval (1s) + execution time + buffer
        await service.StopAsync(cts.Token);

        // Assert
        _blockchain.Chain.Count.Should().BeGreaterThan(1); // Genesis + at least one new block
        _blockchain.PendingTransactions.Count.Should().Be(0); // Transactions moved to block
    }

    [Fact]
    public async Task ExecuteAsync_WithNoActiveValidators_DoesNotCreateBlock()
    {
        // Arrange
        // Don't add any validators to the database

        // Add a pending transaction
        var transaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = "test-public-key",
            PayloadData = "test-payload",
            Signature = "test-signature"
        };
        _blockchain.AddTransaction(transaction);

        var serviceProvider = CreateServiceProvider();
        var service = new BlockCreationBackgroundService(
            serviceProvider,
            _mockLogger.Object,
            _consensusSettings,
            _blockchain);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(1500); // Wait for check
        await service.StopAsync(cts.Token);

        // Assert
        _blockchain.Chain.Count.Should().Be(1); // Only genesis block
        _blockchain.PendingTransactions.Count.Should().Be(1); // Transaction still pending
    }

    [Fact]
    public async Task ExecuteAsync_BelowMinimumTransactions_DoesNotCreateBlock()
    {
        // Arrange
        var strictSettings = new ConsensusSettings
        {
            BlockCreationIntervalSeconds = 1,
            MinimumTransactionsPerBlock = 5, // Require at least 5 transactions
            EnableAutomatedBlockCreation = true,
            ValidatorPassword = "TestPassword123!"
        };

        var validator = CreateTestValidator();
        await Context.Validators.AddAsync(validator);
        await Context.SaveChangesAsync();

        // Add only 3 transactions (below minimum)
        for (int i = 0; i < 3; i++)
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                Type = TransactionType.ShipmentCreated,
                Timestamp = DateTime.UtcNow,
                SenderPublicKey = "test-public-key",
                PayloadData = $"test-payload-{i}",
                Signature = "test-signature"
            };
            _blockchain.AddTransaction(transaction);
        }

        var serviceProvider = CreateServiceProvider();
        var service = new BlockCreationBackgroundService(
            serviceProvider,
            _mockLogger.Object,
            strictSettings,
            _blockchain);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(1500);
        await service.StopAsync(cts.Token);

        // Assert
        _blockchain.Chain.Count.Should().Be(1); // Only genesis block
        _blockchain.PendingTransactions.Count.Should().Be(3); // All transactions still pending
    }

    #endregion

    #region Helper Methods

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // Register DbContext
        services.AddSingleton(Context);

        // Register repositories
        services.AddScoped<IValidatorRepository, ValidatorRepository>();

        // Register cryptography services
        services.AddSingleton<IHashService>(_hashService);
        services.AddSingleton<IDigitalSignatureService>(_signatureService);

        // Register key management service with mock
        var mockKeyManagement = new Mock<IKeyManagementService>();
        mockKeyManagement
            .Setup(x => x.DecryptPrivateKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string encryptedKey, string password) =>
            {
                // For testing, return a valid private key
                var (privateKey, _) = _signatureService.GenerateKeyPair();
                return privateKey;
            });
        services.AddScoped(_ => mockKeyManagement.Object);

        // Register consensus engine
        services.AddScoped<IConsensusEngine, ProofOfAuthorityConsensusEngine>();

        return services.BuildServiceProvider();
    }

    private Validator CreateTestValidator()
    {
        var (privateKey, publicKey) = _signatureService.GenerateKeyPair();

        return new Validator
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Validator",
            PublicKey = publicKey,
            EncryptedPrivateKey = "encrypted-private-key",
            Address = "http://localhost:5000",
            Priority = 1,
            IsActive = true,
            TotalBlocksCreated = 0,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };
    }

    #endregion
}
