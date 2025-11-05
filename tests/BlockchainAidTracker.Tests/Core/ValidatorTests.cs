using BlockchainAidTracker.Core.Models;
using FluentAssertions;
using Xunit;

namespace BlockchainAidTracker.Tests.Core;

public class ValidatorTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var validator = new Validator();

        // Assert
        validator.Id.Should().NotBeNullOrEmpty();
        validator.Name.Should().BeEmpty();
        validator.PublicKey.Should().BeEmpty();
        validator.EncryptedPrivateKey.Should().BeEmpty();
        validator.Address.Should().BeNull();
        validator.IsActive.Should().BeTrue();
        validator.Priority.Should().Be(0);
        validator.TotalBlocksCreated.Should().Be(0);
        validator.CreatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        validator.UpdatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        validator.LastBlockCreatedTimestamp.Should().BeNull();
        validator.Description.Should().BeNull();
    }

    [Fact]
    public void Constructor_Default_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var validator1 = new Validator();
        var validator2 = new Validator();

        // Assert
        validator1.Id.Should().NotBe(validator2.Id);
    }

    [Fact]
    public void Constructor_Parameterized_ShouldInitializeWithProvidedValues()
    {
        // Arrange
        var name = "Validator-1";
        var publicKey = "public-key-123";
        var encryptedPrivateKey = "encrypted-private-key-456";
        var priority = 5;
        var address = "http://validator1:5000";
        var description = "Primary validator node";

        // Act
        var validator = new Validator(name, publicKey, encryptedPrivateKey, priority, address, description);

        // Assert
        validator.Id.Should().NotBeNullOrEmpty();
        validator.Name.Should().Be(name);
        validator.PublicKey.Should().Be(publicKey);
        validator.EncryptedPrivateKey.Should().Be(encryptedPrivateKey);
        validator.Priority.Should().Be(priority);
        validator.Address.Should().Be(address);
        validator.Description.Should().Be(description);
        validator.IsActive.Should().BeTrue();
        validator.TotalBlocksCreated.Should().Be(0);
        validator.LastBlockCreatedTimestamp.Should().BeNull();
        validator.CreatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        validator.UpdatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_Parameterized_WithNullName_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new Validator(null!, "public-key", "encrypted-key", 0);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*name*");
    }

    [Fact]
    public void Constructor_Parameterized_WithNullPublicKey_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new Validator("Validator-1", null!, "encrypted-key", 0);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*publicKey*");
    }

    [Fact]
    public void Constructor_Parameterized_WithNullEncryptedPrivateKey_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new Validator("Validator-1", "public-key", null!, 0);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*encryptedPrivateKey*");
    }

    [Fact]
    public void Constructor_Parameterized_WithNegativePriority_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new Validator("Validator-1", "public-key", "encrypted-key", -1);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Priority must be non-negative*");
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var validator = new Validator();
        validator.Deactivate(); // First deactivate
        var originalUpdatedTimestamp = validator.UpdatedTimestamp;

        // Act
        Thread.Sleep(10); // Ensure time difference
        validator.Activate();

        // Assert
        validator.IsActive.Should().BeTrue();
        validator.UpdatedTimestamp.Should().BeAfter(originalUpdatedTimestamp);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var validator = new Validator();
        var originalUpdatedTimestamp = validator.UpdatedTimestamp;

        // Act
        Thread.Sleep(10); // Ensure time difference
        validator.Deactivate();

        // Assert
        validator.IsActive.Should().BeFalse();
        validator.UpdatedTimestamp.Should().BeAfter(originalUpdatedTimestamp);
    }

    [Fact]
    public void RecordBlockCreation_ShouldIncrementCountAndUpdateTimestamp()
    {
        // Arrange
        var validator = new Validator();
        var originalCount = validator.TotalBlocksCreated;
        var originalUpdatedTimestamp = validator.UpdatedTimestamp;

        // Act
        Thread.Sleep(10); // Ensure time difference
        validator.RecordBlockCreation();

        // Assert
        validator.TotalBlocksCreated.Should().Be(originalCount + 1);
        validator.LastBlockCreatedTimestamp.Should().NotBeNull();
        validator.LastBlockCreatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        validator.UpdatedTimestamp.Should().BeAfter(originalUpdatedTimestamp);
    }

    [Fact]
    public void RecordBlockCreation_CalledMultipleTimes_ShouldIncrementCorrectly()
    {
        // Arrange
        var validator = new Validator();

        // Act
        validator.RecordBlockCreation();
        Thread.Sleep(10);
        validator.RecordBlockCreation();
        Thread.Sleep(10);
        validator.RecordBlockCreation();

        // Assert
        validator.TotalBlocksCreated.Should().Be(3);
        validator.LastBlockCreatedTimestamp.Should().NotBeNull();
    }

    [Fact]
    public void UpdatePriority_WithValidValue_ShouldUpdatePriorityAndTimestamp()
    {
        // Arrange
        var validator = new Validator("Validator-1", "public-key", "encrypted-key", 0);
        var originalUpdatedTimestamp = validator.UpdatedTimestamp;

        // Act
        Thread.Sleep(10); // Ensure time difference
        validator.UpdatePriority(10);

        // Assert
        validator.Priority.Should().Be(10);
        validator.UpdatedTimestamp.Should().BeAfter(originalUpdatedTimestamp);
    }

    [Fact]
    public void UpdatePriority_WithNegativeValue_ShouldThrowArgumentException()
    {
        // Arrange
        var validator = new Validator();

        // Act
        Action act = () => validator.UpdatePriority(-1);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Priority must be non-negative*");
    }

    [Fact]
    public void UpdateAddress_ShouldUpdateAddressAndTimestamp()
    {
        // Arrange
        var validator = new Validator();
        var originalUpdatedTimestamp = validator.UpdatedTimestamp;
        var newAddress = "http://new-validator:5001";

        // Act
        Thread.Sleep(10); // Ensure time difference
        validator.UpdateAddress(newAddress);

        // Assert
        validator.Address.Should().Be(newAddress);
        validator.UpdatedTimestamp.Should().BeAfter(originalUpdatedTimestamp);
    }

    [Fact]
    public void UpdateAddress_WithNull_ShouldAllowNullAddress()
    {
        // Arrange
        var validator = new Validator("Validator-1", "public-key", "encrypted-key", 0, "http://old-address:5000");

        // Act
        validator.UpdateAddress(null);

        // Assert
        validator.Address.Should().BeNull();
    }
}
