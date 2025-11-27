using BlockchainAidTracker.Services.Consensus;
using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Cryptography;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.Interfaces;
using BlockchainAidTracker.Tests.Infrastructure;
using FluentAssertions;
using Moq;

namespace BlockchainAidTracker.Tests.Services.Consensus;

/// <summary>
/// Unit tests for the ProofOfAuthorityConsensusEngine class.
/// </summary>
public class ProofOfAuthorityConsensusEngineTests : DatabaseTestBase
{
    private readonly Mock<IKeyManagementService> _mockKeyManagementService;
    private readonly IDigitalSignatureService _signatureService;
    private readonly IHashService _hashService;
    private readonly ValidatorRepository _validatorRepository;
    private readonly ProofOfAuthorityConsensusEngine _consensusEngine;

    public ProofOfAuthorityConsensusEngineTests()
    {
        _mockKeyManagementService = new Mock<IKeyManagementService>();
        _signatureService = new DigitalSignatureService();
        _hashService = new HashService();
        _validatorRepository = new ValidatorRepository(Context);
        // Use RoundRobin strategy for deterministic test behavior
        _validatorRepository.SetSelectionStrategy(ValidatorSelectionStrategyType.RoundRobin);
        _consensusEngine = new ProofOfAuthorityConsensusEngine(
            _validatorRepository,
            _mockKeyManagementService.Object,
            _signatureService,
            _hashService);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullValidatorRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProofOfAuthorityConsensusEngine(
            null!,
            _mockKeyManagementService.Object,
            _signatureService,
            _hashService);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("validatorRepository");
    }

    [Fact]
    public void Constructor_WithNullKeyManagementService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProofOfAuthorityConsensusEngine(
            _validatorRepository,
            null!,
            _signatureService,
            _hashService);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("keyManagementService");
    }

    [Fact]
    public void Constructor_WithNullSignatureService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProofOfAuthorityConsensusEngine(
            _validatorRepository,
            _mockKeyManagementService.Object,
            null!,
            _hashService);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("signatureService");
    }

    [Fact]
    public void Constructor_WithNullHashService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProofOfAuthorityConsensusEngine(
            _validatorRepository,
            _mockKeyManagementService.Object,
            _signatureService,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("hashService");
    }

    #endregion

    #region CreateBlockAsync Tests

    [Fact]
    public async Task CreateBlockAsync_WithNullBlockchain_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _consensusEngine.CreateBlockAsync(null!, "password");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("blockchain");
    }

    [Fact]
    public async Task CreateBlockAsync_WithNullPassword_ThrowsArgumentException()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Act
        var act = async () => await _consensusEngine.CreateBlockAsync(blockchain, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*password*");
    }

    [Fact]
    public async Task CreateBlockAsync_WithEmptyPassword_ThrowsArgumentException()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Act
        var act = async () => await _consensusEngine.CreateBlockAsync(blockchain, "");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*password*");
    }

    [Fact]
    public async Task CreateBlockAsync_WithNoPendingTransactions_ThrowsInvalidOperationException()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // No transactions added, so PendingTransactions is empty

        // Act
        var act = async () => await _consensusEngine.CreateBlockAsync(blockchain, "password");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pending transactions*");
    }

    [Fact]
    public async Task CreateBlockAsync_WithNoActiveValidators_ThrowsInvalidOperationException()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Add a transaction
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var transaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = publicKey,
            PayloadData = "test-data"
        };
        blockchain.ValidateTransactionSignatures = false;
        blockchain.AddTransaction(transaction);

        // No validators in database

        // Act
        var act = async () => await _consensusEngine.CreateBlockAsync(blockchain, "password");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active validators*");
    }

    [Fact]
    public async Task CreateBlockAsync_WithInvalidPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);

        // Add a transaction
        var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
        var transaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = publicKey,
            PayloadData = "test-data"
        };
        blockchain.ValidateTransactionSignatures = false;
        blockchain.AddTransaction(transaction);

        // Add a validator
        var validator = TestData.CreateValidator()
            .WithName("Validator-1")
            .Build();
        await Context.Validators.AddAsync(validator);
        await Context.SaveChangesAsync();

        // Mock key decryption to throw exception (wrong password)
        _mockKeyManagementService
            .Setup(x => x.DecryptPrivateKey(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("Decryption failed"));

        // Act
        var act = async () => await _consensusEngine.CreateBlockAsync(blockchain, "wrong-password");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to decrypt validator's private key*");
    }

    [Fact]
    public async Task CreateBlockAsync_WithValidInputs_CreatesSignedBlock()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        blockchain.ValidateBlockSignatures = false; // Disable for this test

        // Generate validator key pair
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();

        // Add a validator
        var validator = TestData.CreateValidator()
            .WithName("Validator-1")
            .WithPublicKey(validatorPublicKey)
            .Build();
        await Context.Validators.AddAsync(validator);
        await Context.SaveChangesAsync();

        // Add a transaction
        var (senderPublicKey, senderPrivateKey) = _signatureService.GenerateKeyPair();
        var transaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = senderPublicKey,
            PayloadData = "test-shipment-data"
        };
        blockchain.ValidateTransactionSignatures = false;
        blockchain.AddTransaction(transaction);

        // Mock key decryption
        _mockKeyManagementService
            .Setup(x => x.DecryptPrivateKey(validator.EncryptedPrivateKey, "correct-password"))
            .Returns(validatorPrivateKey);

        // Act
        var block = await _consensusEngine.CreateBlockAsync(blockchain, "correct-password");

        // Assert
        block.Should().NotBeNull();
        block.Index.Should().Be(1); // After genesis block
        block.ValidatorPublicKey.Should().Be(validatorPublicKey);
        block.ValidatorSignature.Should().NotBeNullOrEmpty();
        block.Transactions.Should().HaveCount(1);
        block.Transactions[0].Should().Be(transaction);
        block.PreviousHash.Should().Be(blockchain.Chain[0].Hash);
        block.Hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateBlockAsync_UpdatesValidatorStatistics()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        blockchain.ValidateBlockSignatures = false;

        // Generate validator key pair
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();

        // Add a validator with initial statistics
        var validator = TestData.CreateValidator()
            .WithName("Validator-1")
            .WithPublicKey(validatorPublicKey)
            .WithBlocksCreated(5)
            .Build();
        await Context.Validators.AddAsync(validator);
        await Context.SaveChangesAsync();

        var initialBlocksCreated = validator.TotalBlocksCreated;

        // Add a transaction
        var (senderPublicKey, senderPrivateKey) = _signatureService.GenerateKeyPair();
        var transaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = senderPublicKey,
            PayloadData = "test-data"
        };
        blockchain.ValidateTransactionSignatures = false;
        blockchain.AddTransaction(transaction);

        // Mock key decryption
        _mockKeyManagementService
            .Setup(x => x.DecryptPrivateKey(validator.EncryptedPrivateKey, "password"))
            .Returns(validatorPrivateKey);

        // Act
        await _consensusEngine.CreateBlockAsync(blockchain, "password");

        // Assert - Verify validator statistics were updated
        DetachAllEntities();
        var updatedValidator = await Context.Validators.FindAsync(validator.Id);
        updatedValidator.Should().NotBeNull();
        updatedValidator!.TotalBlocksCreated.Should().Be(initialBlocksCreated + 1);
        updatedValidator.LastBlockCreatedTimestamp.Should().NotBeNull();
        updatedValidator.LastBlockCreatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateBlockAsync_SelectsValidatorsInRoundRobinOrder()
    {
        // Arrange
        var blockchain = new BlockchainAidTracker.Blockchain.Blockchain(_hashService, _signatureService);
        blockchain.ValidateBlockSignatures = false;

        // Create 3 validators with different priorities
        var validators = new List<Validator>();
        for (int i = 0; i < 3; i++)
        {
            var (publicKey, privateKey) = _signatureService.GenerateKeyPair();
            var validator = TestData.CreateValidator()
                .WithName($"Validator-{i}")
                .WithPublicKey(publicKey)
                .WithPriority(i)
                .Build();
            validators.Add(validator);

            // Mock key decryption for this validator
            _mockKeyManagementService
                .Setup(x => x.DecryptPrivateKey(validator.EncryptedPrivateKey, "password"))
                .Returns(privateKey);
        }

        await Context.Validators.AddRangeAsync(validators);
        await Context.SaveChangesAsync();

        // Act - Create 3 blocks and verify round-robin selection
        var selectedValidators = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            // Add transaction
            var (senderPublicKey, senderPrivateKey) = _signatureService.GenerateKeyPair();
            var transaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                Type = TransactionType.ShipmentCreated,
                Timestamp = DateTime.UtcNow,
                SenderPublicKey = senderPublicKey,
                PayloadData = $"test-data-{i}"
            };
            blockchain.ValidateTransactionSignatures = false;
            blockchain.AddTransaction(transaction);

            var block = await _consensusEngine.CreateBlockAsync(blockchain, "password");
            selectedValidators.Add(block.ValidatorPublicKey);

            // Add block to blockchain for next iteration
            blockchain.AddBlock(block);
        }

        // Assert - Round-robin should cycle through all validators: 0, 1, 2
        selectedValidators.Should().HaveCount(3);
        selectedValidators[0].Should().Be(validators[0].PublicKey);
        selectedValidators[1].Should().Be(validators[1].PublicKey);
        selectedValidators[2].Should().Be(validators[2].PublicKey);
    }

    #endregion

    #region ValidateBlock Tests

    [Fact]
    public void ValidateBlock_WithNullBlock_ReturnsFalse()
    {
        // Arrange
        var previousBlock = new Block { Index = 0, Hash = "previous-hash" };

        // Act
        var result = _consensusEngine.ValidateBlock(null!, previousBlock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateBlock_WithNullPreviousBlock_ReturnsFalse()
    {
        // Arrange
        var block = new Block { Index = 1, PreviousHash = "previous-hash" };

        // Act
        var result = _consensusEngine.ValidateBlock(block, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateBlock_WithIncorrectIndex_ReturnsFalse()
    {
        // Arrange
        var previousBlock = new Block { Index = 5, Hash = "previous-hash" };
        var block = new Block { Index = 10, PreviousHash = "previous-hash" }; // Should be 6

        // Act
        var result = _consensusEngine.ValidateBlock(block, previousBlock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateBlock_WithIncorrectPreviousHash_ReturnsFalse()
    {
        // Arrange
        var previousBlock = new Block { Index = 5, Hash = "correct-hash" };
        var block = new Block { Index = 6, PreviousHash = "wrong-hash" };

        // Act
        var result = _consensusEngine.ValidateBlock(block, previousBlock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateBlock_WithInvalidHash_ReturnsFalse()
    {
        // Arrange
        var previousBlock = new Block { Index = 0, Hash = "previous-hash" };
        var block = new Block
        {
            Index = 1,
            Timestamp = DateTime.UtcNow,
            Transactions = new List<Transaction>(),
            PreviousHash = "previous-hash",
            Hash = "wrong-hash",
            ValidatorPublicKey = "validator-public-key"
        };

        // Act
        var result = _consensusEngine.ValidateBlock(block, previousBlock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateBlock_WithInvalidValidatorSignature_ReturnsFalse()
    {
        // Arrange
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();
        var previousBlock = new Block { Index = 0, Hash = "previous-hash" };

        var block = new Block
        {
            Index = 1,
            Timestamp = DateTime.UtcNow,
            Transactions = new List<Transaction>(),
            PreviousHash = previousBlock.Hash,
            ValidatorPublicKey = validatorPublicKey,
            ValidatorSignature = "invalid-signature"
        };

        // Calculate correct hash
        block.Hash = _hashService.ComputeSha256Hash(block.CalculateHashData());

        // Act
        var result = _consensusEngine.ValidateBlock(block, previousBlock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateBlock_WithValidBlockAndSignature_ReturnsTrue()
    {
        // Arrange
        var (validatorPublicKey, validatorPrivateKey) = _signatureService.GenerateKeyPair();
        var previousBlock = new Block { Index = 0, Hash = "previous-hash" };

        var block = new Block
        {
            Index = 1,
            Timestamp = DateTime.UtcNow,
            Transactions = new List<Transaction>(),
            PreviousHash = previousBlock.Hash,
            ValidatorPublicKey = validatorPublicKey
        };

        // Calculate hash and sign block
        block.Hash = _hashService.ComputeSha256Hash(block.CalculateHashData());
        var dataToSign = $"{block.Index}{block.Hash}{block.Timestamp:O}{block.ValidatorPublicKey}";
        block.ValidatorSignature = _signatureService.SignData(dataToSign, validatorPrivateKey);

        // Act
        var result = _consensusEngine.ValidateBlock(block, previousBlock);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetCurrentBlockProposerAsync Tests

    [Fact]
    public async Task GetCurrentBlockProposerAsync_WithNoValidators_ReturnsNull()
    {
        // Act
        var result = await _consensusEngine.GetCurrentBlockProposerAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentBlockProposerAsync_WithActiveValidators_ReturnsValidatorId()
    {
        // Arrange
        var validator = TestData.CreateValidator()
            .WithName("Validator-1")
            .Build();
        await Context.Validators.AddAsync(validator);
        await Context.SaveChangesAsync();

        // Act
        var result = await _consensusEngine.GetCurrentBlockProposerAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(validator.Id);
    }

    [Fact]
    public async Task GetCurrentBlockProposerAsync_WithMultipleValidators_ReturnsRandomSelection()
    {
        // Arrange
        var validator1 = TestData.CreateValidator()
            .WithName("Validator-1")
            .WithPriority(0)
            .Build();
        var validator2 = TestData.CreateValidator()
            .WithName("Validator-2")
            .WithPriority(1)
            .Build();

        await Context.Validators.AddRangeAsync(validator1, validator2);
        await Context.SaveChangesAsync();

        // Act - Call multiple times to verify randomness (statistical test)
        var selectedValidators = new HashSet<string>();
        for (int i = 0; i < 10; i++)
        {
            var proposer = await _consensusEngine.GetCurrentBlockProposerAsync();
            selectedValidators.Add(proposer!);
        }

        // Assert - Both validators should be selected at least once in 10 calls
        selectedValidators.Should().Contain(new[] { validator1.Id, validator2.Id });
    }

    #endregion

    #region RecordBlockCreationAsync Tests

    [Fact]
    public async Task RecordBlockCreationAsync_WithNullValidatorId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _consensusEngine.RecordBlockCreationAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Validator ID*");
    }

    [Fact]
    public async Task RecordBlockCreationAsync_WithEmptyValidatorId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _consensusEngine.RecordBlockCreationAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Validator ID*");
    }

    [Fact]
    public async Task RecordBlockCreationAsync_WithNonExistentValidator_ThrowsInvalidOperationException()
    {
        // Act
        var act = async () => await _consensusEngine.RecordBlockCreationAsync("non-existent-id");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task RecordBlockCreationAsync_WithValidValidator_UpdatesStatistics()
    {
        // Arrange
        var validator = TestData.CreateValidator()
            .WithName("Validator-1")
            .WithBlocksCreated(10)
            .Build();
        await Context.Validators.AddAsync(validator);
        await Context.SaveChangesAsync();

        var initialBlocksCreated = validator.TotalBlocksCreated;

        // Act
        await _consensusEngine.RecordBlockCreationAsync(validator.Id);

        // Assert
        DetachAllEntities();
        var updatedValidator = await Context.Validators.FindAsync(validator.Id);
        updatedValidator.Should().NotBeNull();
        updatedValidator!.TotalBlocksCreated.Should().Be(initialBlocksCreated + 1);
        updatedValidator.LastBlockCreatedTimestamp.Should().NotBeNull();
        updatedValidator.LastBlockCreatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RecordBlockCreationAsync_CalledMultipleTimes_IncrementsCorrectly()
    {
        // Arrange
        var validator = TestData.CreateValidator()
            .WithName("Validator-1")
            .Build();
        await Context.Validators.AddAsync(validator);
        await Context.SaveChangesAsync();

        // Act
        await _consensusEngine.RecordBlockCreationAsync(validator.Id);
        await _consensusEngine.RecordBlockCreationAsync(validator.Id);
        await _consensusEngine.RecordBlockCreationAsync(validator.Id);

        // Assert
        DetachAllEntities();
        var updatedValidator = await Context.Validators.FindAsync(validator.Id);
        updatedValidator.Should().NotBeNull();
        updatedValidator!.TotalBlocksCreated.Should().Be(3);
    }

    #endregion
}
