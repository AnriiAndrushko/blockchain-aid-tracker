using BlockchainAidTracker.Core.Models;
using FluentAssertions;
using Xunit;

namespace BlockchainAidTracker.Tests.Core;

public class AuditLogTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var auditLog = new AuditLog();

        // Assert
        auditLog.Id.Should().NotBeNullOrEmpty();
        auditLog.Description.Should().BeEmpty();
        auditLog.IsSuccess.Should().BeTrue();
        auditLog.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_Default_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var auditLog1 = new AuditLog();
        var auditLog2 = new AuditLog();

        // Assert
        auditLog1.Id.Should().NotBe(auditLog2.Id);
    }

    [Fact]
    public void Constructor_Parameterized_ShouldInitializeWithProvidedValues()
    {
        // Arrange
        var category = AuditLogCategory.Authentication;
        var action = AuditLogAction.UserLoggedIn;
        var description = "User logged in successfully";
        var userId = "user-123";
        var username = "testuser";
        var entityId = "entity-456";
        var entityType = "User";

        // Act
        var auditLog = new AuditLog(category, action, description, userId, username, entityId, entityType);

        // Assert
        auditLog.Id.Should().NotBeNullOrEmpty();
        auditLog.Category.Should().Be(category);
        auditLog.Action.Should().Be(action);
        auditLog.Description.Should().Be(description);
        auditLog.UserId.Should().Be(userId);
        auditLog.Username.Should().Be(username);
        auditLog.EntityId.Should().Be(entityId);
        auditLog.EntityType.Should().Be(entityType);
        auditLog.IsSuccess.Should().BeTrue();
        auditLog.ErrorMessage.Should().BeNull();
        auditLog.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_Parameterized_WithNullDescription_ShouldThrowArgumentNullException()
    {
        // Arrange
        var category = AuditLogCategory.Authentication;
        var action = AuditLogAction.UserLoggedIn;

        // Act
        Action act = () => new AuditLog(category, action, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("description");
    }

    [Fact]
    public void Success_Factory_ShouldCreateSuccessfulAuditLog()
    {
        // Arrange
        var category = AuditLogCategory.Shipment;
        var action = AuditLogAction.ShipmentCreated;
        var description = "New shipment created";
        var userId = "user-123";
        var entityId = "shipment-789";

        // Act
        var auditLog = AuditLog.Success(category, action, description, userId, entityId: entityId);

        // Assert
        auditLog.Category.Should().Be(category);
        auditLog.Action.Should().Be(action);
        auditLog.Description.Should().Be(description);
        auditLog.UserId.Should().Be(userId);
        auditLog.EntityId.Should().Be(entityId);
        auditLog.IsSuccess.Should().BeTrue();
        auditLog.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Failure_Factory_ShouldCreateFailedAuditLog()
    {
        // Arrange
        var category = AuditLogCategory.Blockchain;
        var action = AuditLogAction.BlockCreated;
        var description = "Block creation failed";
        var errorMessage = "Insufficient validators";
        var userId = "validator-123";

        // Act
        var auditLog = AuditLog.Failure(category, action, description, errorMessage, userId);

        // Assert
        auditLog.Category.Should().Be(category);
        auditLog.Action.Should().Be(action);
        auditLog.Description.Should().Be(description);
        auditLog.UserId.Should().Be(userId);
        auditLog.IsSuccess.Should().BeFalse();
        auditLog.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void AuditLog_WithAllOptionalFields_ShouldStoreAllData()
    {
        // Arrange & Act
        var auditLog = new AuditLog
        {
            Category = AuditLogCategory.UserManagement,
            Action = AuditLogAction.UserActivated,
            Description = "User account activated",
            UserId = "admin-123",
            Username = "admin",
            EntityId = "user-456",
            EntityType = "User",
            Metadata = "{\"previousStatus\":\"inactive\"}",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            IsSuccess = true,
            Timestamp = DateTime.UtcNow
        };

        // Assert
        auditLog.Category.Should().Be(AuditLogCategory.UserManagement);
        auditLog.Action.Should().Be(AuditLogAction.UserActivated);
        auditLog.Metadata.Should().NotBeNullOrEmpty();
        auditLog.IpAddress.Should().Be("192.168.1.1");
        auditLog.UserAgent.Should().Be("Mozilla/5.0");
    }

    [Theory]
    [InlineData(AuditLogCategory.Authentication)]
    [InlineData(AuditLogCategory.UserManagement)]
    [InlineData(AuditLogCategory.Shipment)]
    [InlineData(AuditLogCategory.Blockchain)]
    [InlineData(AuditLogCategory.Validator)]
    [InlineData(AuditLogCategory.SmartContract)]
    public void AuditLog_ShouldSupportAllCategories(AuditLogCategory category)
    {
        // Arrange & Act
        var auditLog = new AuditLog
        {
            Category = category,
            Action = AuditLogAction.UserRegistered,
            Description = "Test"
        };

        // Assert
        auditLog.Category.Should().Be(category);
    }

    [Theory]
    [InlineData(AuditLogAction.UserRegistered)]
    [InlineData(AuditLogAction.UserLoggedIn)]
    [InlineData(AuditLogAction.ShipmentCreated)]
    [InlineData(AuditLogAction.BlockCreated)]
    [InlineData(AuditLogAction.ValidatorRegistered)]
    [InlineData(AuditLogAction.SmartContractExecuted)]
    public void AuditLog_ShouldSupportAllActions(AuditLogAction action)
    {
        // Arrange & Act
        var auditLog = new AuditLog
        {
            Category = AuditLogCategory.Authentication,
            Action = action,
            Description = "Test"
        };

        // Assert
        auditLog.Action.Should().Be(action);
    }
}
