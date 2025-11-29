using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlockchainAidTracker.Tests.DataAccess;

/// <summary>
/// Unit tests for ShipmentLocationRepository
/// </summary>
public class ShipmentLocationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ShipmentLocationRepository _repository;

    public ShipmentLocationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ShipmentLocationRepository(_context);
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
    public async Task AddAsync_WithValidLocation_ShouldAddSuccessfully()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();
        var location = new ShipmentLocation(shipment.Id, 10.5m, 20.5m, "Test Location", user.Id, 5.0m);

        // Act
        await _repository.AddAsync(location);

        // Assert
        var saved = await _repository.GetByIdAsync(location.Id);
        Assert.NotNull(saved);
        Assert.Equal(location.Id, saved.Id);
        Assert.Equal(shipment.Id, saved.ShipmentId);
        Assert.Equal(10.5m, saved.Latitude);
        Assert.Equal(20.5m, saved.Longitude);
    }

    [Fact]
    public async Task GetLatestAsync_WithMultipleLocations_ShouldReturnMostRecent()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();
        var location1 = new ShipmentLocation(shipment.Id, 10.0m, 20.0m, "Location1", user.Id);
        var location2 = new ShipmentLocation(shipment.Id, 15.0m, 25.0m, "Location2", user.Id);

        await _repository.AddAsync(location1);
        await Task.Delay(10); // Ensure different timestamps
        await _repository.AddAsync(location2);

        // Act
        var latest = await _repository.GetLatestAsync(shipment.Id);

        // Assert
        Assert.NotNull(latest);
        Assert.Equal(location2.Id, latest.Id);
        Assert.Equal("Location2", latest.LocationName);
    }

    [Fact]
    public async Task GetLatestAsync_WithNoLocations_ShouldReturnNull()
    {
        // Arrange
        var shipmentId = Guid.NewGuid().ToString();

        // Act
        var result = await _repository.GetLatestAsync(shipmentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnLocationsOrderedByTimestamp()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();
        var location1 = new ShipmentLocation(shipment.Id, 10.0m, 20.0m, "Location1", user.Id);
        var location2 = new ShipmentLocation(shipment.Id, 15.0m, 25.0m, "Location2", user.Id);
        var location3 = new ShipmentLocation(shipment.Id, 20.0m, 30.0m, "Location3", user.Id);

        await _repository.AddAsync(location1);
        await Task.Delay(5);
        await _repository.AddAsync(location2);
        await Task.Delay(5);
        await _repository.AddAsync(location3);

        // Act
        var history = await _repository.GetHistoryAsync(shipment.Id);

        // Assert
        Assert.Equal(3, history.Count);
        Assert.Equal(location1.Id, history[0].Id);
        Assert.Equal(location2.Id, history[1].Id);
        Assert.Equal(location3.Id, history[2].Id);
    }

    [Fact]
    public async Task GetHistoryByDateRangeAsync_ShouldReturnLocationsInRange()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();
        var now = DateTime.UtcNow;

        var location1 = new ShipmentLocation(shipment.Id, 10.0m, 20.0m, "Location1", user.Id);
        await _repository.AddAsync(location1);

        await Task.Delay(100);

        var location2 = new ShipmentLocation(shipment.Id, 15.0m, 25.0m, "Location2", user.Id);
        await _repository.AddAsync(location2);

        // Act
        var history = await _repository.GetHistoryByDateRangeAsync(
            shipment.Id,
            now.AddSeconds(-1),
            DateTime.UtcNow.AddSeconds(1));

        // Assert
        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task GetHistoryByDateRangeAsync_OutsideRange_ShouldReturnEmpty()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();
        var location = new ShipmentLocation(shipment.Id, 10.0m, 20.0m, "Location1", user.Id);
        await _repository.AddAsync(location);

        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(2);

        // Act
        var history = await _repository.GetHistoryByDateRangeAsync(shipment.Id, startDate, endDate);

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public async Task GetPaginatedAsync_ShouldReturnCorrectPage()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();

        for (int i = 0; i < 15; i++)
        {
            var location = new ShipmentLocation(
                shipment.Id,
                10.0m + i,
                20.0m + i,
                $"Location{i}",
                user.Id);
            await _repository.AddAsync(location);
        }

        // Act
        var page1 = await _repository.GetPaginatedAsync(shipment.Id, 1, 10);
        var page2 = await _repository.GetPaginatedAsync(shipment.Id, 2, 10);

        // Assert
        Assert.Equal(10, page1.Count);
        Assert.Equal(5, page2.Count);
    }

    [Fact]
    public async Task GetPaginatedAsync_ShouldOrderByMostRecent()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var user = CreateTestUser();

        var location1 = new ShipmentLocation(shipment.Id, 10.0m, 20.0m, "Location1", user.Id);
        await _repository.AddAsync(location1);
        await Task.Delay(5);
        var location2 = new ShipmentLocation(shipment.Id, 15.0m, 25.0m, "Location2", user.Id);
        await _repository.AddAsync(location2);

        // Act
        var page = await _repository.GetPaginatedAsync(shipment.Id, 1, 10);

        // Assert
        Assert.Equal(location2.Id, page[0].Id); // Most recent first
        Assert.Equal(location1.Id, page[1].Id);
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnCorrectCount()
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
            await _repository.AddAsync(location);
        }

        // Act
        var count = await _repository.GetCountAsync(shipment.Id);

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task GetCountAsync_WithNoLocations_ShouldReturnZero()
    {
        // Arrange
        var shipmentId = Guid.NewGuid().ToString();

        // Act
        var count = await _repository.GetCountAsync(shipmentId);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetLatestAsync_WithInvalidShipmentId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetLatestAsync(string.Empty));
    }

    [Fact]
    public async Task GetHistoryAsync_WithInvalidShipmentId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetHistoryAsync(null!));
    }
}
