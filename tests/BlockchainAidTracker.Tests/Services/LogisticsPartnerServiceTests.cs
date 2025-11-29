using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.LogisticsPartner;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BlockchainAidTracker.Tests.Services;

/// <summary>
/// Unit tests for LogisticsPartnerService
/// Tests location tracking, delivery event recording, and role-based access control
/// </summary>
public class LogisticsPartnerServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly LogisticsPartnerService _service;
    private readonly Mock<ILogger<LogisticsPartnerService>> _mockLogger;

    public LogisticsPartnerServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<LogisticsPartnerService>>();

        var shipmentRepository = new ShipmentRepository(_context);
        var locationRepository = new ShipmentLocationRepository(_context);
        var eventRepository = new DeliveryEventRepository(_context);
        var userRepository = new UserRepository(_context);

        _service = new LogisticsPartnerService(
            shipmentRepository,
            locationRepository,
            eventRepository,
            userRepository,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private User CreateTestUser(UserRole role = UserRole.LogisticsPartner)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = $"user_{Guid.NewGuid()}",
            PublicKey = Guid.NewGuid().ToString(),
            Role = role
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    private Shipment CreateTestShipment(ShipmentStatus status = ShipmentStatus.InTransit)
    {
        var shipment = new Shipment
        {
            Id = Guid.NewGuid().ToString(),
            Origin = "Origin A",
            Destination = "Destination A",
            AssignedRecipient = "Recipient1",
            ExpectedDeliveryTimeframe = "2024-12-25",
            CoordinatorPublicKey = "Coordinator1",
            Status = status
        };
        _context.Shipments.Add(shipment);
        _context.SaveChanges();
        return shipment;
    }

    #region GetAssignedShipmentsAsync Tests

    [Fact]
    public async Task GetAssignedShipmentsAsync_WithValidLogisticsPartner_ShouldReturnShipments()
    {
        // Arrange
        var logisticsPartner = CreateTestUser(UserRole.LogisticsPartner);
        var shipment1 = CreateTestShipment(ShipmentStatus.InTransit);
        var shipment2 = CreateTestShipment(ShipmentStatus.Delivered);
        var shipment3 = CreateTestShipment(ShipmentStatus.Confirmed);

        // Act
        var result = await _service.GetAssignedShipmentsAsync(logisticsPartner.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, s => s.Id == shipment1.Id);
        Assert.Contains(result, s => s.Id == shipment2.Id);
        Assert.Contains(result, s => s.Id == shipment3.Id);
    }

    [Fact]
    public async Task GetAssignedShipmentsAsync_WithAdminUser_ShouldReturnShipments()
    {
        // Arrange
        var admin = CreateTestUser(UserRole.Administrator);
        var shipment = CreateTestShipment(ShipmentStatus.InTransit);

        // Act
        var result = await _service.GetAssignedShipmentsAsync(admin.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetAssignedShipmentsAsync_WithStatusFilter_ShouldReturnOnlyFilteredStatus()
    {
        // Arrange
        var logisticsPartner = CreateTestUser(UserRole.LogisticsPartner);
        CreateTestShipment(ShipmentStatus.InTransit);
        CreateTestShipment(ShipmentStatus.InTransit);
        CreateTestShipment(ShipmentStatus.Delivered);

        // Act
        var result = await _service.GetAssignedShipmentsAsync(logisticsPartner.Id, "2"); // InTransit = 2

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(ShipmentStatus.InTransit, s.Status));
    }

    [Fact]
    public async Task GetAssignedShipmentsAsync_WithNonLogisticsPartnerRole_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var coordinator = CreateTestUser(UserRole.Coordinator);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.GetAssignedShipmentsAsync(coordinator.Id));
    }

    [Fact]
    public async Task GetAssignedShipmentsAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetAssignedShipmentsAsync(null!));
    }

    [Fact]
    public async Task GetAssignedShipmentsAsync_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetAssignedShipmentsAsync(string.Empty));
    }

    [Fact]
    public async Task GetAssignedShipmentsAsync_WithNonExistentUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetAssignedShipmentsAsync(nonExistentUserId));
    }

    [Fact]
    public async Task GetAssignedShipmentsAsync_WithNoShipments_ShouldReturnEmptyList()
    {
        // Arrange
        var logisticsPartner = CreateTestUser(UserRole.LogisticsPartner);

        // Act
        var result = await _service.GetAssignedShipmentsAsync(logisticsPartner.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetShipmentLocationAsync Tests

    [Fact]
    public async Task GetShipmentLocationAsync_WithExistingLocation_ShouldReturnLocation()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();
        var location = new ShipmentLocation(shipment.Id, 10.5m, 20.5m, "Warehouse A", user.Id, 5.0m);
        _context.ShipmentLocations.Add(location);
        _context.SaveChanges();

        // Act
        var result = await _service.GetShipmentLocationAsync(shipment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(location.Id, result.Id);
        Assert.Equal(10.5m, result.Latitude);
        Assert.Equal(20.5m, result.Longitude);
    }

    [Fact]
    public async Task GetShipmentLocationAsync_WithNoLocation_ShouldReturnNull()
    {
        // Arrange
        var shipmentId = Guid.NewGuid().ToString();

        // Act
        var result = await _service.GetShipmentLocationAsync(shipmentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetShipmentLocationAsync_WithNullShipmentId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetShipmentLocationAsync(null!));
    }

    #endregion

    #region UpdateLocationAsync Tests

    [Fact]
    public async Task UpdateLocationAsync_WithValidRequest_ShouldCreateLocationAndEvent()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();
        var request = new UpdateLocationRequest
        {
            Latitude = 15.5m,
            Longitude = 25.5m,
            LocationName = "Distribution Center",
            GpsAccuracy = 3.5m
        };

        // Act
        var result = await _service.UpdateLocationAsync(shipment.Id, user.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(15.5m, result.Latitude);
        Assert.Equal(25.5m, result.Longitude);
        Assert.Equal("Distribution Center", result.LocationName);

        // Verify delivery event was created
        var events = await _context.DeliveryEvents
            .Where(e => e.ShipmentId == shipment.Id)
            .ToListAsync();
        Assert.Single(events);
        Assert.Equal(DeliveryEventType.LocationUpdated, events[0].EventType);
    }

    [Fact]
    public async Task UpdateLocationAsync_WithAdminUser_ShouldSucceed()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var admin = CreateTestUser(UserRole.Administrator);
        var request = new UpdateLocationRequest
        {
            Latitude = 15.5m,
            Longitude = 25.5m,
            LocationName = "Admin Location",
            GpsAccuracy = null
        };

        // Act
        var result = await _service.UpdateLocationAsync(shipment.Id, admin.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(15.5m, result.Latitude);
    }

    [Fact]
    public async Task UpdateLocationAsync_WithInvalidCoordinates_ShouldThrowBusinessException()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();
        var request = new UpdateLocationRequest
        {
            Latitude = 95.0m, // Invalid: > 90
            Longitude = 25.5m,
            LocationName = "Invalid Location"
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.UpdateLocationAsync(shipment.Id, user.Id, request));
    }

    [Fact]
    public async Task UpdateLocationAsync_WithNonLogisticsPartner_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var coordinator = CreateTestUser(UserRole.Coordinator);
        var request = new UpdateLocationRequest
        {
            Latitude = 15.5m,
            Longitude = 25.5m,
            LocationName = "Test Location"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.UpdateLocationAsync(shipment.Id, coordinator.Id, request));
    }

    [Fact]
    public async Task UpdateLocationAsync_WithNonExistentShipment_ShouldThrowNotFoundException()
    {
        // Arrange
        var user = CreateTestUser();
        var nonExistentShipmentId = Guid.NewGuid().ToString();
        var request = new UpdateLocationRequest
        {
            Latitude = 15.5m,
            Longitude = 25.5m,
            LocationName = "Test Location"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateLocationAsync(nonExistentShipmentId, user.Id, request));
    }

    [Fact]
    public async Task UpdateLocationAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.UpdateLocationAsync(shipment.Id, user.Id, null!));
    }

    #endregion

    #region ConfirmDeliveryInitiationAsync Tests

    [Fact]
    public async Task ConfirmDeliveryInitiationAsync_WithInTransitShipment_ShouldCreateDeliveryStartedEvent()
    {
        // Arrange
        var shipment = CreateTestShipment(ShipmentStatus.InTransit);
        var user = CreateTestUser();

        // Act
        var result = await _service.ConfirmDeliveryInitiationAsync(shipment.Id, user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DeliveryEventType.DeliveryStarted, result.EventType);

        // Verify event in database
        var events = await _context.DeliveryEvents
            .Where(e => e.ShipmentId == shipment.Id)
            .ToListAsync();
        Assert.Single(events);
    }

    [Fact]
    public async Task ConfirmDeliveryInitiationAsync_WithNonInTransitShipment_ShouldThrowBusinessException()
    {
        // Arrange
        var shipment = CreateTestShipment(ShipmentStatus.Created);
        var user = CreateTestUser();

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.ConfirmDeliveryInitiationAsync(shipment.Id, user.Id));
    }

    [Fact]
    public async Task ConfirmDeliveryInitiationAsync_WithNonExistentShipment_ShouldThrowNotFoundException()
    {
        // Arrange
        var user = CreateTestUser();
        var nonExistentShipmentId = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.ConfirmDeliveryInitiationAsync(nonExistentShipmentId, user.Id));
    }

    #endregion

    #region GetDeliveryHistoryAsync Tests

    [Fact]
    public async Task GetDeliveryHistoryAsync_WithMultipleEvents_ShouldReturnAllEventsChronologically()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();

        var event1 = new DeliveryEvent(shipment.Id, DeliveryEventType.DeliveryStarted, "Started", user.Id);
        var event2 = new DeliveryEvent(shipment.Id, DeliveryEventType.LocationUpdated, "Updated", user.Id);
        var event3 = new DeliveryEvent(shipment.Id, DeliveryEventType.Delivered, "Delivered", user.Id);

        _context.DeliveryEvents.Add(event1);
        await Task.Delay(5);
        _context.DeliveryEvents.Add(event2);
        await Task.Delay(5);
        _context.DeliveryEvents.Add(event3);
        _context.SaveChanges();

        // Act
        var result = await _service.GetDeliveryHistoryAsync(shipment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(event1.Id, result[0].Id);
        Assert.Equal(event2.Id, result[1].Id);
        Assert.Equal(event3.Id, result[2].Id);
    }

    [Fact]
    public async Task GetDeliveryHistoryAsync_WithNoEvents_ShouldReturnEmptyList()
    {
        // Arrange
        var shipment = CreateTestShipment();

        // Act
        var result = await _service.GetDeliveryHistoryAsync(shipment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDeliveryHistoryAsync_WithNonExistentShipment_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentShipmentId = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetDeliveryHistoryAsync(nonExistentShipmentId));
    }

    #endregion

    #region GetLocationHistoryAsync Tests

    [Fact]
    public async Task GetLocationHistoryAsync_WithValidShipment_ShouldReturnLocationHistory()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();

        for (int i = 0; i < 5; i++)
        {
            var location = new ShipmentLocation(
                shipment.Id,
                10.0m + i,
                20.0m + i,
                $"Location{i}",
                user.Id);
            _context.ShipmentLocations.Add(location);
        }
        _context.SaveChanges();

        // Act
        var result = await _service.GetLocationHistoryAsync(shipment.Id, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetLocationHistoryAsync_WithLimitParameter_ShouldReturnLimitedResults()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();

        for (int i = 0; i < 10; i++)
        {
            var location = new ShipmentLocation(
                shipment.Id,
                10.0m + i,
                20.0m + i,
                $"Location{i}",
                user.Id);
            _context.ShipmentLocations.Add(location);
        }
        _context.SaveChanges();

        // Act
        var result = await _service.GetLocationHistoryAsync(shipment.Id, 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetLocationHistoryAsync_WithInvalidLimit_ShouldThrowArgumentException()
    {
        // Arrange
        var shipment = CreateTestShipment();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetLocationHistoryAsync(shipment.Id, 0));
    }

    [Fact]
    public async Task GetLocationHistoryAsync_WithNonExistentShipment_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentShipmentId = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetLocationHistoryAsync(nonExistentShipmentId));
    }

    #endregion

    #region ReportDeliveryIssueAsync Tests

    [Fact]
    public async Task ReportDeliveryIssueAsync_WithValidRequest_ShouldCreateIssueReportedEvent()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();
        var request = new ReportIssueRequest
        {
            IssueType = IssueType.Delay,
            Description = "Shipment delayed due to weather",
            Priority = IssuePriority.High
        };

        // Act
        var result = await _service.ReportDeliveryIssueAsync(shipment.Id, user.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DeliveryEventType.IssueReported, result.EventType);
        Assert.Contains("Shipment delayed due to weather", result.Description);

        // Verify event in database
        var events = await _context.DeliveryEvents
            .Where(e => e.ShipmentId == shipment.Id)
            .ToListAsync();
        Assert.Single(events);
    }

    [Fact]
    public async Task ReportDeliveryIssueAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.ReportDeliveryIssueAsync(shipment.Id, user.Id, null!));
    }

    [Fact]
    public async Task ReportDeliveryIssueAsync_WithNonExistentShipment_ShouldThrowNotFoundException()
    {
        // Arrange
        var user = CreateTestUser();
        var nonExistentShipmentId = Guid.NewGuid().ToString();
        var request = new ReportIssueRequest
        {
            IssueType = IssueType.Damage,
            Description = "Damaged shipment",
            Priority = IssuePriority.High
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.ReportDeliveryIssueAsync(nonExistentShipmentId, user.Id, request));
    }

    #endregion

    #region ConfirmReceiptAsync Tests

    [Fact]
    public async Task ConfirmReceiptAsync_WithValidShipment_ShouldCreateReceiptConfirmedEvent()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();

        // Act
        var result = await _service.ConfirmReceiptAsync(shipment.Id, user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DeliveryEventType.ReceiptConfirmed, result.EventType);

        // Verify event in database
        var events = await _context.DeliveryEvents
            .Where(e => e.ShipmentId == shipment.Id)
            .ToListAsync();
        Assert.Single(events);
    }

    [Fact]
    public async Task ConfirmReceiptAsync_WithNonExistentShipment_ShouldThrowNotFoundException()
    {
        // Arrange
        var user = CreateTestUser();
        var nonExistentShipmentId = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.ConfirmReceiptAsync(nonExistentShipmentId, user.Id));
    }

    [Fact]
    public async Task ConfirmReceiptAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var shipment = CreateTestShipment();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ConfirmReceiptAsync(shipment.Id, null!));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CompleteDeliveryWorkflow_AllOperationsSucceed()
    {
        // Arrange
        var shipment = CreateTestShipment(ShipmentStatus.InTransit);
        var logisticsPartner = CreateTestUser(UserRole.LogisticsPartner);

        // Act - Step 1: Confirm delivery initiation
        var initiationEvent = await _service.ConfirmDeliveryInitiationAsync(shipment.Id, logisticsPartner.Id);
        Assert.Equal(DeliveryEventType.DeliveryStarted, initiationEvent.EventType);

        // Act - Step 2: Update location multiple times
        var location1Request = new UpdateLocationRequest
        {
            Latitude = 10.0m,
            Longitude = 20.0m,
            LocationName = "Warehouse",
            GpsAccuracy = 5.0m
        };
        var location1 = await _service.UpdateLocationAsync(shipment.Id, logisticsPartner.Id, location1Request);
        Assert.NotNull(location1);

        await Task.Delay(50);

        var location2Request = new UpdateLocationRequest
        {
            Latitude = 15.0m,
            Longitude = 25.0m,
            LocationName = "In Transit",
            GpsAccuracy = 10.0m
        };
        var location2 = await _service.UpdateLocationAsync(shipment.Id, logisticsPartner.Id, location2Request);
        Assert.NotNull(location2);

        // Act - Step 3: Report an issue
        var issueRequest = new ReportIssueRequest
        {
            IssueType = IssueType.Delay,
            Description = "Minor delay",
            Priority = IssuePriority.Low
        };
        var issueEvent = await _service.ReportDeliveryIssueAsync(shipment.Id, logisticsPartner.Id, issueRequest);
        Assert.Equal(DeliveryEventType.IssueReported, issueEvent.EventType);

        // Act - Step 4: Confirm receipt
        var receiptEvent = await _service.ConfirmReceiptAsync(shipment.Id, logisticsPartner.Id);
        Assert.Equal(DeliveryEventType.ReceiptConfirmed, receiptEvent.EventType);

        // Assert - Verify full history
        var history = await _service.GetDeliveryHistoryAsync(shipment.Id);
        Assert.Equal(5, history.Count); // 1 start + 2 location updates + 1 issue + 1 receipt

        var locationHistory = await _service.GetLocationHistoryAsync(shipment.Id, 10);
        Assert.Equal(2, locationHistory.Count);

        var currentLocation = await _service.GetShipmentLocationAsync(shipment.Id);
        Assert.NotNull(currentLocation);
        Assert.Equal("In Transit", currentLocation.LocationName);
    }

    #endregion
}
