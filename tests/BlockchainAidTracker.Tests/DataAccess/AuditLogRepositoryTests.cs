using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BlockchainAidTracker.Tests.DataAccess;

public class AuditLogRepositoryTests : DatabaseTestBase
{
    private readonly AuditLogRepository _auditLogRepository;

    public AuditLogRepositoryTests()
    {
        _auditLogRepository = new AuditLogRepository(Context);
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ShouldAddAuditLogToDatabase()
    {
        // Arrange
        var auditLog = new AuditLog(
            AuditLogCategory.Authentication,
            AuditLogAction.UserLoggedIn,
            "User logged in successfully",
            "user-123",
            "testuser");

        // Act
        var result = await _auditLogRepository.AddAsync(auditLog);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be("User logged in successfully");

        // Verify it's in database
        using var newContext = CreateNewContext();
        var dbLog = await newContext.AuditLogs.FindAsync(auditLog.Id);
        dbLog.Should().NotBeNull();
        dbLog!.UserId.Should().Be("user-123");
    }

    #endregion

    #region GetByCategoryAsync Tests

    [Fact]
    public async Task GetByCategoryAsync_ShouldReturnLogsInCategory()
    {
        // Arrange
        var authLog = AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, "Login");
        var shipmentLog = AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Created");
        var authLog2 = AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserRegistered, "Register");

        await Context.AuditLogs.AddRangeAsync(authLog, shipmentLog, authLog2);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _auditLogRepository.GetByCategoryAsync(AuditLogCategory.Authentication);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(log => log.Category.Should().Be(AuditLogCategory.Authentication));
    }

    #endregion

    #region GetByActionAsync Tests

    [Fact]
    public async Task GetByActionAsync_ShouldReturnLogsWithAction()
    {
        // Arrange
        var log1 = AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Created 1");
        var log2 = AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentStatusUpdated, "Updated");
        var log3 = AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Created 2");

        await Context.AuditLogs.AddRangeAsync(log1, log2, log3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _auditLogRepository.GetByActionAsync(AuditLogAction.ShipmentCreated);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(log => log.Action.Should().Be(AuditLogAction.ShipmentCreated));
    }

    #endregion

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnLogsForUser()
    {
        // Arrange
        var log1 = AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, "Login", "user-123");
        var log2 = AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Created", "user-456");
        var log3 = AuditLog.Success(AuditLogCategory.UserManagement, AuditLogAction.UserProfileUpdated, "Updated", "user-123");

        await Context.AuditLogs.AddRangeAsync(log1, log2, log3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _auditLogRepository.GetByUserIdAsync("user-123");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(log => log.UserId.Should().Be("user-123"));
    }

    #endregion

    #region GetByEntityIdAsync Tests

    [Fact]
    public async Task GetByEntityIdAsync_ShouldReturnLogsForEntity()
    {
        // Arrange
        var log1 = AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Created", entityId: "shipment-123");
        var log2 = AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentStatusUpdated, "Updated", entityId: "shipment-456");
        var log3 = AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentDeliveryConfirmed, "Delivered", entityId: "shipment-123");

        await Context.AuditLogs.AddRangeAsync(log1, log2, log3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _auditLogRepository.GetByEntityIdAsync("shipment-123");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(log => log.EntityId.Should().Be("shipment-123"));
    }

    #endregion

    #region GetByEntityTypeAsync Tests

    [Fact]
    public async Task GetByEntityTypeAsync_ShouldReturnLogsForEntityType()
    {
        // Arrange
        var log1 = new AuditLog(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Created", entityType: "Shipment");
        var log2 = new AuditLog(AuditLogCategory.UserManagement, AuditLogAction.UserActivated, "Activated", entityType: "User");
        var log3 = new AuditLog(AuditLogCategory.Shipment, AuditLogAction.ShipmentStatusUpdated, "Updated", entityType: "Shipment");

        await Context.AuditLogs.AddRangeAsync(log1, log2, log3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _auditLogRepository.GetByEntityTypeAsync("Shipment");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(log => log.EntityType.Should().Be("Shipment"));
    }

    #endregion

    #region GetByDateRangeAsync Tests

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnLogsWithinRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-5);
        var endDate = DateTime.UtcNow.AddDays(-1);

        var log1 = new AuditLog(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, "Login 1") { Timestamp = DateTime.UtcNow.AddDays(-3) };
        var log2 = new AuditLog(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, "Login 2") { Timestamp = DateTime.UtcNow.AddDays(-10) };
        var log3 = new AuditLog(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, "Login 3") { Timestamp = DateTime.UtcNow.AddDays(-2) };

        await Context.AuditLogs.AddRangeAsync(log1, log2, log3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _auditLogRepository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(log => log.Timestamp.Should().BeOnOrAfter(startDate).And.BeOnOrBefore(endDate));
    }

    #endregion

    #region GetFailedLogsAsync Tests

    [Fact]
    public async Task GetFailedLogsAsync_ShouldReturnOnlyFailedLogs()
    {
        // Arrange
        var successLog = AuditLog.Success(AuditLogCategory.Blockchain, AuditLogAction.BlockCreated, "Block created");
        var failureLog1 = AuditLog.Failure(AuditLogCategory.Blockchain, AuditLogAction.BlockCreated, "Block failed", "Error message");
        var failureLog2 = AuditLog.Failure(AuditLogCategory.SmartContract, AuditLogAction.SmartContractExecuted, "Contract failed", "Execution error");

        await Context.AuditLogs.AddRangeAsync(successLog, failureLog1, failureLog2);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _auditLogRepository.GetFailedLogsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(log => log.IsSuccess.Should().BeFalse());
    }

    #endregion

    #region GetRecentLogsAsync Tests

    [Fact]
    public async Task GetRecentLogsAsync_ShouldReturnLogsWithPagination()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var log = AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, $"Login {i}");
            log.Timestamp = DateTime.UtcNow.AddMinutes(-i);
            await Context.AuditLogs.AddAsync(log);
        }
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _auditLogRepository.GetRecentLogsAsync(pageSize: 5, pageNumber: 1);

        // Assert
        result.Should().HaveCount(5);
        // Should be ordered by timestamp descending
        result[0].Timestamp.Should().BeAfter(result[4].Timestamp);
    }

    [Fact]
    public async Task GetRecentLogsAsync_SecondPage_ShouldReturnCorrectLogs()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var log = AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, $"Login {i}");
            await Context.AuditLogs.AddAsync(log);
        }
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _auditLogRepository.GetRecentLogsAsync(pageSize: 5, pageNumber: 2);

        // Assert
        result.Should().HaveCount(5);
    }

    #endregion

    #region GetCountByCategoryAsync Tests

    [Fact]
    public async Task GetCountByCategoryAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var authLog1 = AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, "Login 1");
        var authLog2 = AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserRegistered, "Register");
        var shipmentLog = AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Created");

        await Context.AuditLogs.AddRangeAsync(authLog1, authLog2, shipmentLog);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _auditLogRepository.GetCountByCategoryAsync(AuditLogCategory.Authentication);

        // Assert
        result.Should().Be(2);
    }

    #endregion

    #region GetFilteredLogsAsync Tests

    [Fact]
    public async Task GetFilteredLogsAsync_WithMultipleFilters_ShouldReturnMatchingLogs()
    {
        // Arrange
        var log1 = AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Created", "user-123", entityId: "shipment-1");
        var log2 = AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentStatusUpdated, "Updated", "user-456", entityId: "shipment-2");
        var log3 = AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Created 2", "user-123", entityId: "shipment-3");

        await Context.AuditLogs.AddRangeAsync(log1, log2, log3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _auditLogRepository.GetFilteredLogsAsync(
            category: AuditLogCategory.Shipment,
            action: AuditLogAction.ShipmentCreated,
            userId: "user-123");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(log =>
        {
            log.Category.Should().Be(AuditLogCategory.Shipment);
            log.Action.Should().Be(AuditLogAction.ShipmentCreated);
            log.UserId.Should().Be("user-123");
        });
    }

    [Fact]
    public async Task GetFilteredLogsAsync_WithSuccessFilter_ShouldReturnOnlySuccessfulLogs()
    {
        // Arrange
        var successLog = AuditLog.Success(AuditLogCategory.Blockchain, AuditLogAction.BlockCreated, "Success");
        var failureLog = AuditLog.Failure(AuditLogCategory.Blockchain, AuditLogAction.BlockCreated, "Failure", "Error");

        await Context.AuditLogs.AddRangeAsync(successLog, failureLog);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _auditLogRepository.GetFilteredLogsAsync(isSuccess: true);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsSuccess.Should().BeTrue();
    }

    #endregion
}
