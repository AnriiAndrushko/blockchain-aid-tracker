using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Tests.Infrastructure;
using Xunit;

namespace BlockchainAidTracker.Tests.DataAccess;

public class ValidatorRepositoryTests : DatabaseTestBase
{
    private readonly ValidatorRepository _validatorRepository;

    public ValidatorRepositoryTests()
    {
        _validatorRepository = new ValidatorRepository(Context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddValidatorToDatabase()
    {
        // Arrange
        var validator = TestData.CreateValidator()
            .WithName("Validator-1")
            .Build();

        // Act
        var result = await _validatorRepository.AddAsync(validator);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Validator-1", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingValidator_ShouldReturnValidator()
    {
        // Arrange
        var validator = TestData.CreateValidator()
            .WithName("UniqueValidator")
            .Build();
        await Context.Validators.AddAsync(validator);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _validatorRepository.GetByNameAsync("UniqueValidator");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("UniqueValidator", result.Name);
    }

    [Fact]
    public async Task GetByPublicKeyAsync_ExistingValidator_ShouldReturnValidator()
    {
        // Arrange
        var validator = TestData.CreateValidator()
            .WithPublicKey("unique-public-key-123")
            .Build();
        await Context.Validators.AddAsync(validator);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _validatorRepository.GetByPublicKeyAsync("unique-public-key-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("unique-public-key-123", result.PublicKey);
    }

    [Fact]
    public async Task GetActiveValidatorsAsync_ShouldReturnOnlyActiveValidatorsOrderedByPriority()
    {
        // Arrange
        var validator1 = TestData.CreateValidator().WithName("V1").WithPriority(2).Build();
        var validator2 = TestData.CreateValidator().WithName("V2").WithPriority(1).Build();
        var validator3 = TestData.CreateValidator().WithName("V3").WithPriority(3).AsInactive().Build();

        await Context.Validators.AddRangeAsync(validator1, validator2, validator3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _validatorRepository.GetActiveValidatorsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("V2", result[0].Name); // Priority 1
        Assert.Equal("V1", result[1].Name); // Priority 2
    }

    [Fact]
    public async Task GetNextValidatorForBlockCreationAsync_ShouldReturnValidatorWithOldestBlockCreation()
    {
        // Arrange
        var validator1 = TestData.CreateValidator()
            .WithName("V1")
            .WithBlocksCreated(5)
            .Build();
        validator1.LastBlockCreatedTimestamp = DateTime.UtcNow.AddHours(-2);

        var validator2 = TestData.CreateValidator()
            .WithName("V2")
            .WithBlocksCreated(3)
            .Build();
        validator2.LastBlockCreatedTimestamp = DateTime.UtcNow.AddHours(-1);

        var validator3 = TestData.CreateValidator()
            .WithName("V3")
            .Build(); // No blocks created yet

        await Context.Validators.AddRangeAsync(validator1, validator2, validator3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _validatorRepository.GetNextValidatorForBlockCreationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("V3", result.Name); // Never created a block
    }

    [Fact]
    public async Task NameExistsAsync_ExistingName_ShouldReturnTrue()
    {
        // Arrange
        var validator = TestData.CreateValidator().WithName("ExistingValidator").Build();
        await Context.Validators.AddAsync(validator);
        await Context.SaveChangesAsync();

        // Act
        var result = await _validatorRepository.NameExistsAsync("ExistingValidator");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task NameExistsAsync_NonExistingName_ShouldReturnFalse()
    {
        // Act
        var result = await _validatorRepository.NameExistsAsync("NonExistingValidator");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetActiveValidatorCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var validator1 = TestData.CreateValidator().Build();
        var validator2 = TestData.CreateValidator().Build();
        var validator3 = TestData.CreateValidator().AsInactive().Build();

        await Context.Validators.AddRangeAsync(validator1, validator2, validator3);
        await Context.SaveChangesAsync();

        // Act
        var result = await _validatorRepository.GetActiveValidatorCountAsync();

        // Assert
        Assert.Equal(2, result);
    }
}
