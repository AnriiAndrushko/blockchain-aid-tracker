using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.AuditLog;
using BlockchainAidTracker.Services.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BlockchainAidTracker.Tests.Services;

/// <summary>
/// Unit tests for AuditLogService
/// </summary>
public class AuditLogServiceTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly AuditLogService _auditLogService;

    public AuditLogServiceTests()
    {
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _auditLogService = new AuditLogService(_auditLogRepositoryMock.Object);
    }

    #region LogAsync Tests

    [Fact]
    public async Task LogAsync_ShouldCreateSuccessfulAuditLog()
    {
        // Arrange
        AuditLog? capturedLog = null;
        _auditLogRepositoryMock.Setup(x => x.AddAsync(It.IsAny<AuditLog>(), default))
            .Callback<AuditLog, CancellationToken>((log, _) => capturedLog = log)
            .ReturnsAsync((AuditLog log, CancellationToken _) => log);

        // Act
        await _auditLogService.LogAsync(
            AuditLogCategory.Authentication,
            AuditLogAction.UserLoggedIn,
            "User logged in successfully",
            "user-123",
            "testuser");

        // Assert
        capturedLog.Should().NotBeNull();
        capturedLog!.Category.Should().Be(AuditLogCategory.Authentication);
        capturedLog.Action.Should().Be(AuditLogAction.UserLoggedIn);
        capturedLog.Description.Should().Be("User logged in successfully");
        capturedLog.UserId.Should().Be("user-123");
        capturedLog.Username.Should().Be("testuser");
        capturedLog.IsSuccess.Should().BeTrue();
        capturedLog.ErrorMessage.Should().BeNull();

        _auditLogRepositoryMock.Verify(x => x.AddAsync(It.IsAny<AuditLog>(), default), Times.Once);
    }

    [Fact]
    public async Task LogAsync_WithAllOptionalParameters_ShouldCreateCompleteAuditLog()
    {
        // Arrange
        AuditLog? capturedLog = null;
        _auditLogRepositoryMock.Setup(x => x.AddAsync(It.IsAny<AuditLog>(), default))
            .Callback<AuditLog, CancellationToken>((log, _) => capturedLog = log)
            .ReturnsAsync((AuditLog log, CancellationToken _) => log);

        // Act
        await _auditLogService.LogAsync(
            AuditLogCategory.Shipment,
            AuditLogAction.ShipmentCreated,
            "Shipment created",
            "user-123",
            "coordinator",
            "shipment-456",
            "Shipment",
            "{\"items\": 5}",
            "192.168.1.1",
            "Mozilla/5.0");

        // Assert
        capturedLog.Should().NotBeNull();
        capturedLog!.EntityId.Should().Be("shipment-456");
        capturedLog.EntityType.Should().Be("Shipment");
        capturedLog.Metadata.Should().Be("{\"items\": 5}");
        capturedLog.IpAddress.Should().Be("192.168.1.1");
        capturedLog.UserAgent.Should().Be("Mozilla/5.0");
    }

    #endregion

    #region LogFailureAsync Tests

    [Fact]
    public async Task LogFailureAsync_ShouldCreateFailedAuditLog()
    {
        // Arrange
        AuditLog? capturedLog = null;
        _auditLogRepositoryMock.Setup(x => x.AddAsync(It.IsAny<AuditLog>(), default))
            .Callback<AuditLog, CancellationToken>((log, _) => capturedLog = log)
            .ReturnsAsync((AuditLog log, CancellationToken _) => log);

        // Act
        await _auditLogService.LogFailureAsync(
            AuditLogCategory.Blockchain,
            AuditLogAction.BlockCreated,
            "Block creation failed",
            "Insufficient validators",
            "validator-123");

        // Assert
        capturedLog.Should().NotBeNull();
        capturedLog!.Category.Should().Be(AuditLogCategory.Blockchain);
        capturedLog.Action.Should().Be(AuditLogAction.BlockCreated);
        capturedLog.Description.Should().Be("Block creation failed");
        capturedLog.ErrorMessage.Should().Be("Insufficient validators");
        capturedLog.UserId.Should().Be("validator-123");
        capturedLog.IsSuccess.Should().BeFalse();

        _auditLogRepositoryMock.Verify(x => x.AddAsync(It.IsAny<AuditLog>(), default), Times.Once);
    }

    #endregion

    #region GetLogsAsync Tests

    [Fact]
    public async Task GetLogsAsync_WithFilter_ShouldReturnFilteredLogs()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Created 1"),
            AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Created 2")
        };

        _auditLogRepositoryMock.Setup(x => x.GetFilteredLogsAsync(
            It.IsAny<AuditLogCategory?>(),
            It.IsAny<AuditLogAction?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            default))
            .ReturnsAsync(logs);

        var filter = new AuditLogFilterRequest
        {
            Category = AuditLogCategory.Shipment,
            PageSize = 50,
            PageNumber = 1
        };

        // Act
        var result = await _auditLogService.GetLogsAsync(filter);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto => dto.Category.Should().Be(AuditLogCategory.Shipment));
    }

    [Fact]
    public async Task GetLogsAsync_NullFilter_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _auditLogService.GetLogsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("filter");
    }

    #endregion

    #region GetLogsByCategoryAsync Tests

    [Fact]
    public async Task GetLogsByCategoryAsync_ShouldReturnLogsInCategory()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, "Login 1"),
            AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserRegistered, "Register")
        };

        _auditLogRepositoryMock.Setup(x => x.GetByCategoryAsync(AuditLogCategory.Authentication, default))
            .ReturnsAsync(logs);

        // Act
        var result = await _auditLogService.GetLogsByCategoryAsync(AuditLogCategory.Authentication);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto => dto.Category.Should().Be(AuditLogCategory.Authentication));
    }

    #endregion

    #region GetLogsByUserIdAsync Tests

    [Fact]
    public async Task GetLogsByUserIdAsync_ValidUserId_ShouldReturnUserLogs()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, "Login", "user-123")
        };

        _auditLogRepositoryMock.Setup(x => x.GetByUserIdAsync("user-123", default))
            .ReturnsAsync(logs);

        // Act
        var result = await _auditLogService.GetLogsByUserIdAsync("user-123");

        // Assert
        result.Should().HaveCount(1);
        result[0].UserId.Should().Be("user-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetLogsByUserIdAsync_InvalidUserId_ShouldThrowArgumentException(string? userId)
    {
        // Act
        Func<Task> act = async () => await _auditLogService.GetLogsByUserIdAsync(userId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    #endregion

    #region GetLogsByEntityIdAsync Tests

    [Fact]
    public async Task GetLogsByEntityIdAsync_ValidEntityId_ShouldReturnEntityLogs()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Created", entityId: "shipment-123")
        };

        _auditLogRepositoryMock.Setup(x => x.GetByEntityIdAsync("shipment-123", default))
            .ReturnsAsync(logs);

        // Act
        var result = await _auditLogService.GetLogsByEntityIdAsync("shipment-123");

        // Assert
        result.Should().HaveCount(1);
        result[0].EntityId.Should().Be("shipment-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetLogsByEntityIdAsync_InvalidEntityId_ShouldThrowArgumentException(string? entityId)
    {
        // Act
        Func<Task> act = async () => await _auditLogService.GetLogsByEntityIdAsync(entityId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("entityId");
    }

    #endregion

    #region GetFailedLogsAsync Tests

    [Fact]
    public async Task GetFailedLogsAsync_ShouldReturnOnlyFailedLogs()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            AuditLog.Failure(AuditLogCategory.Blockchain, AuditLogAction.BlockCreated, "Failed", "Error 1"),
            AuditLog.Failure(AuditLogCategory.SmartContract, AuditLogAction.SmartContractExecuted, "Failed", "Error 2")
        };

        _auditLogRepositoryMock.Setup(x => x.GetFailedLogsAsync(default))
            .ReturnsAsync(logs);

        // Act
        var result = await _auditLogService.GetFailedLogsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto => dto.IsSuccess.Should().BeFalse());
    }

    #endregion

    #region GetRecentLogsAsync Tests

    [Fact]
    public async Task GetRecentLogsAsync_WithPagination_ShouldReturnPagedLogs()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, "Login 1"),
            AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, "Login 2"),
            AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, "Login 3")
        };

        _auditLogRepositoryMock.Setup(x => x.GetRecentLogsAsync(10, 1, default))
            .ReturnsAsync(logs);

        // Act
        var result = await _auditLogService.GetRecentLogsAsync(10, 1);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetRecentLogsAsync_WithDefaultParameters_ShouldUseDefaults()
    {
        // Arrange
        var logs = new List<AuditLog>();
        _auditLogRepositoryMock.Setup(x => x.GetRecentLogsAsync(50, 1, default))
            .ReturnsAsync(logs);

        // Act
        var result = await _auditLogService.GetRecentLogsAsync();

        // Assert
        _auditLogRepositoryMock.Verify(x => x.GetRecentLogsAsync(50, 1, default), Times.Once);
    }

    #endregion

    #region GetCountByCategoryAsync Tests

    [Fact]
    public async Task GetCountByCategoryAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        _auditLogRepositoryMock.Setup(x => x.GetCountByCategoryAsync(AuditLogCategory.Shipment, default))
            .ReturnsAsync(42);

        // Act
        var result = await _auditLogService.GetCountByCategoryAsync(AuditLogCategory.Shipment);

        // Assert
        result.Should().Be(42);
    }

    #endregion
}
