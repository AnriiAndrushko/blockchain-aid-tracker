using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlockchainAidTracker.Tests.DataAccess;

/// <summary>
/// Unit tests for DeliveryEventRepository
/// </summary>
public class DeliveryEventRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DeliveryEventRepository _repository;

    public DeliveryEventRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new DeliveryEventRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private Shipment CreateTestShipment()
    {
        var shipment = new Shipment
        {
            Id = Guid.NewGuid().ToString(),
            Origin = "Origin A",
            Destination = "Destination A",
            AssignedRecipient = "Recipient1",
            ExpectedDeliveryTimeframe = "2024-12-25",
            CoordinatorPublicKey = "Coordinator1",
            Status = ShipmentStatus.InTransit
        };
        _context.Shipments.Add(shipment);
        _context.SaveChanges();
        return shipment;
    }

    private User CreateTestUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = $"user_{Guid.NewGuid()}",
            PublicKey = Guid.NewGuid().ToString(),
            Role = UserRole.LogisticsPartner
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    [Fact]
    public async Task AddAsync_WithValidEvent_ShouldAddSuccessfully()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();
        var deliveryEvent = new DeliveryEvent(
            shipment.Id,
            DeliveryEventType.LocationUpdated,
            "Location updated to warehouse",
            user.Id);

        // Act
        await _repository.AddAsync(deliveryEvent);

        // Assert
        var saved = await _repository.GetByIdAsync(deliveryEvent.Id);
        Assert.NotNull(saved);
        Assert.Equal(deliveryEvent.Id, saved.Id);
        Assert.Equal(DeliveryEventType.LocationUpdated, saved.EventType);
    }

    [Fact]
    public async Task GetByShipmentAsync_ShouldReturnEventsChronologically()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();

        var event1 = new DeliveryEvent(shipment.Id, DeliveryEventType.DeliveryStarted, "Started", user.Id);
        var event2 = new DeliveryEvent(shipment.Id, DeliveryEventType.LocationUpdated, "Updated", user.Id);
        var event3 = new DeliveryEvent(shipment.Id, DeliveryEventType.Delivered, "Delivered", user.Id);

        await _repository.AddAsync(event1);
        await Task.Delay(5);
        await _repository.AddAsync(event2);
        await Task.Delay(5);
        await _repository.AddAsync(event3);

        // Act
        var events = await _repository.GetByShipmentAsync(shipment.Id);

        // Assert
        Assert.Equal(3, events.Count);
        Assert.Equal(event1.Id, events[0].Id);
        Assert.Equal(event2.Id, events[1].Id);
        Assert.Equal(event3.Id, events[2].Id);
    }

    [Fact]
    public async Task GetByEventTypeAsync_ShouldReturnOnlySpecificType()
    {
        // Arrange
        var shipment1 = CreateTestShipment();
        var shipment2 = CreateTestShipment();
        var user = CreateTestUser();

        var event1 = new DeliveryEvent(shipment1.Id, DeliveryEventType.LocationUpdated, "Updated", user.Id);
        var event2 = new DeliveryEvent(shipment2.Id, DeliveryEventType.LocationUpdated, "Updated", user.Id);
        var event3 = new DeliveryEvent(shipment1.Id, DeliveryEventType.DeliveryStarted, "Started", user.Id);

        await _repository.AddAsync(event1);
        await _repository.AddAsync(event2);
        await _repository.AddAsync(event3);

        // Act
        var locationEvents = await _repository.GetByEventTypeAsync(DeliveryEventType.LocationUpdated);

        // Assert
        Assert.Equal(2, locationEvents.Count);
        Assert.All(locationEvents, e => Assert.Equal(DeliveryEventType.LocationUpdated, e.EventType));
    }

    [Fact]
    public async Task GetRecentAsync_ShouldReturnMostRecentEvents()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();

        for (int i = 0; i < 10; i++)
        {
            var deliveryEvent = new DeliveryEvent(
                shipment.Id,
                DeliveryEventType.LocationUpdated,
                $"Location {i}",
                user.Id);
            await _repository.AddAsync(deliveryEvent);
            await Task.Delay(5);
        }

        // Act
        var recent = await _repository.GetRecentAsync(shipment.Id, 3);

        // Assert
        Assert.Equal(3, recent.Count);
        // Should be in descending order (most recent first)
        for (int i = 0; i < recent.Count - 1; i++)
        {
            Assert.True(recent[i].CreatedTimestamp >= recent[i + 1].CreatedTimestamp);
        }
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnEventsInRange()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();
        var now = DateTime.UtcNow;

        var event1 = new DeliveryEvent(shipment.Id, DeliveryEventType.DeliveryStarted, "Started", user.Id);
        await _repository.AddAsync(event1);

        await Task.Delay(100);

        var event2 = new DeliveryEvent(shipment.Id, DeliveryEventType.LocationUpdated, "Updated", user.Id);
        await _repository.AddAsync(event2);

        // Act
        var events = await _repository.GetByDateRangeAsync(
            shipment.Id,
            now.AddSeconds(-1),
            DateTime.UtcNow.AddSeconds(1));

        // Assert
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public async Task GetByDateRangeAsync_OutsideRange_ShouldReturnEmpty()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();
        var deliveryEvent = new DeliveryEvent(shipment.Id, DeliveryEventType.DeliveryStarted, "Started", user.Id);
        await _repository.AddAsync(deliveryEvent);

        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(2);

        // Act
        var events = await _repository.GetByDateRangeAsync(shipment.Id, startDate, endDate);

        // Assert
        Assert.Empty(events);
    }

    [Fact]
    public async Task GetByShipmentAndTypeAsync_ShouldReturnSpecificTypeForShipment()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();

        var event1 = new DeliveryEvent(shipment.Id, DeliveryEventType.LocationUpdated, "Updated1", user.Id);
        var event2 = new DeliveryEvent(shipment.Id, DeliveryEventType.DeliveryStarted, "Started", user.Id);
        var event3 = new DeliveryEvent(shipment.Id, DeliveryEventType.LocationUpdated, "Updated2", user.Id);

        await _repository.AddAsync(event1);
        await _repository.AddAsync(event2);
        await _repository.AddAsync(event3);

        // Act
        var locationEvents = await _repository.GetByShipmentAndTypeAsync(
            shipment.Id,
            DeliveryEventType.LocationUpdated);

        // Assert
        Assert.Equal(2, locationEvents.Count);
        Assert.All(locationEvents, e => Assert.Equal(DeliveryEventType.LocationUpdated, e.EventType));
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();

        for (int i = 0; i < 5; i++)
        {
            var deliveryEvent = new DeliveryEvent(
                shipment.Id,
                DeliveryEventType.LocationUpdated,
                $"Event {i}",
                user.Id);
            await _repository.AddAsync(deliveryEvent);
        }

        // Act
        var count = await _repository.GetCountAsync(shipment.Id);

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task GetCountAsync_WithNoEvents_ShouldReturnZero()
    {
        // Arrange
        var shipmentId = Guid.NewGuid().ToString();

        // Act
        var count = await _repository.GetCountAsync(shipmentId);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetByShipmentAsync_WithInvalidShipmentId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetByShipmentAsync(string.Empty));
    }

    [Fact]
    public async Task GetRecentAsync_WithInvalidCount_ShouldThrowArgumentException()
    {
        // Arrange
        var shipmentId = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetRecentAsync(shipmentId, 0));
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithInvalidDateRange_ShouldThrowArgumentException()
    {
        // Arrange
        var shipmentId = Guid.NewGuid().ToString();
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repository.GetByDateRangeAsync(shipmentId, startDate, endDate));
    }
}
