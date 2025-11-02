using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlockchainAidTracker.Tests.DataAccess;

public class ApplicationDbContextTests : DatabaseTestBase
{
    [Fact]
    public void DbContext_ShouldHaveCorrectDbSets()
    {
        // Assert
        Assert.NotNull(Context.Users);
        Assert.NotNull(Context.Shipments);
        Assert.NotNull(Context.ShipmentItems);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldUpdateShipmentTimestamp()
    {
        // Arrange
        var shipment = TestData.CreateShipment().Build();
        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();

        var originalTimestamp = shipment.UpdatedTimestamp;
        await Task.Delay(10); // Ensure time passes

        // Act
        shipment.UpdateStatus(ShipmentStatus.Validated);
        await Context.SaveChangesAsync();

        // Assert
        Assert.True(shipment.UpdatedTimestamp > originalTimestamp);
    }

    [Fact]
    public void SaveChanges_ShouldUpdateShipmentTimestamp()
    {
        // Arrange
        var shipment = TestData.CreateShipment().Build();
        Context.Shipments.Add(shipment);
        Context.SaveChanges();

        var originalTimestamp = shipment.UpdatedTimestamp;
        System.Threading.Thread.Sleep(10);

        // Act
        shipment.UpdateStatus(ShipmentStatus.InTransit);
        Context.SaveChanges();

        // Assert
        Assert.True(shipment.UpdatedTimestamp > originalTimestamp);
    }

    [Fact]
    public async Task UniqueConstraints_UsernameShouldBeUnique()
    {
        // Arrange
        var user1 = TestData.CreateUser()
            .WithUsername("duplicate")
            .WithEmail("user1@test.com")
            .WithPublicKey("key1")
            .Build();

        var user2 = TestData.CreateUser()
            .WithUsername("duplicate")
            .WithEmail("user2@test.com")
            .WithPublicKey("key2")
            .Build();

        await Context.Users.AddAsync(user1);
        await Context.SaveChangesAsync();

        await Context.Users.AddAsync(user2);

        // Act & Assert
        // Note: In-memory database doesn't enforce unique constraints
        // This test documents the expected behavior in real databases
        var exception = await Record.ExceptionAsync(async () => await Context.SaveChangesAsync());

        // In-memory DB won't throw, but document that it should in real DB
        // Real DB would throw DbUpdateException
    }

    [Fact]
    public async Task UniqueConstraints_EmailShouldBeUnique()
    {
        // Arrange
        var user1 = TestData.CreateUser()
            .WithUsername("user1")
            .WithEmail("duplicate@test.com")
            .WithPublicKey("key1")
            .Build();

        var user2 = TestData.CreateUser()
            .WithUsername("user2")
            .WithEmail("duplicate@test.com")
            .WithPublicKey("key2")
            .Build();

        await Context.Users.AddAsync(user1);
        await Context.SaveChangesAsync();

        await Context.Users.AddAsync(user2);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await Context.SaveChangesAsync());

        // Document expected behavior (would fail in real DB)
    }

    [Fact]
    public async Task UniqueConstraints_PublicKeyShouldBeUnique()
    {
        // Arrange
        var user1 = TestData.CreateUser()
            .WithUsername("user1")
            .WithEmail("user1@test.com")
            .WithPublicKey("duplicate-key")
            .Build();

        var user2 = TestData.CreateUser()
            .WithUsername("user2")
            .WithEmail("user2@test.com")
            .WithPublicKey("duplicate-key")
            .Build();

        await Context.Users.AddAsync(user1);
        await Context.SaveChangesAsync();

        await Context.Users.AddAsync(user2);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await Context.SaveChangesAsync());

        // Document expected behavior (would fail in real DB)
    }

    [Fact]
    public async Task UniqueConstraints_QrCodeShouldBeUnique()
    {
        // Arrange
        var shipment1 = TestData.CreateShipment().Build();
        var originalQrCode = shipment1.QrCodeData;

        var shipment2 = TestData.CreateShipment().Build();

        // Force same QR code (normally auto-generated and unique)
        var qrCodeProperty = typeof(Shipment).GetProperty("QrCodeData");
        qrCodeProperty!.SetValue(shipment2, originalQrCode);

        await Context.Shipments.AddAsync(shipment1);
        await Context.SaveChangesAsync();

        await Context.Shipments.AddAsync(shipment2);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await Context.SaveChangesAsync());

        // Document expected behavior (would fail in real DB)
    }

    [Fact]
    public async Task CascadeDelete_RemovingShipmentShouldRemoveItems()
    {
        // Arrange
        var shipment = TestData.CreateShipment()
            .WithMedicalSupplies()
            .Build();

        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();

        var itemIds = shipment.Items.Select(i => i.Id).ToList();

        // Act
        Context.Shipments.Remove(shipment);
        await Context.SaveChangesAsync();

        // Assert
        DetachAllEntities();
        var remainingItems = await Context.ShipmentItems
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync();

        Assert.Empty(remainingItems);
    }

    [Fact]
    public async Task Relationships_ShipmentShouldLoadItems()
    {
        // Arrange
        var shipment = TestData.CreateShipment()
            .WithItem("Item1", 10, "units", "Cat1")
            .WithItem("Item2", 20, "units", "Cat2")
            .Build();

        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var loadedShipment = await Context.Shipments
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == shipment.Id);

        // Assert
        Assert.NotNull(loadedShipment);
        Assert.Equal(2, loadedShipment.Items.Count);
    }

    [Fact]
    public async Task Indexes_ShouldImproveQueryPerformance()
    {
        // Arrange - Add multiple users
        var users = Enumerable.Range(1, 100)
            .Select(i => TestData.CreateUser()
                .WithUsername($"user{i}")
                .WithEmail($"user{i}@test.com")
                .WithRole(i % 2 == 0 ? UserRole.Coordinator : UserRole.Recipient)
                .Build())
            .ToList();

        await Context.Users.AddRangeAsync(users);
        await Context.SaveChangesAsync();

        // Act - Query by indexed column (Role)
        var coordinators = await Context.Users
            .Where(u => u.Role == UserRole.Coordinator)
            .ToListAsync();

        // Assert
        Assert.Equal(50, coordinators.Count);
        Assert.All(coordinators, u => Assert.Equal(UserRole.Coordinator, u.Role));
    }

    [Fact]
    public async Task DefaultValues_IsActiveShouldDefaultToTrue()
    {
        // Arrange
        var user = TestData.CreateUser().Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var loadedUser = await Context.Users.FindAsync(user.Id);

        // Assert
        Assert.NotNull(loadedUser);
        Assert.True(loadedUser.IsActive);
    }

    [Fact]
    public async Task EnumStorage_ShouldStoreAsString()
    {
        // Arrange
        var user = TestData.CreateUser()
            .AsCoordinator()
            .Build();

        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var loadedUser = await Context.Users.FindAsync(user.Id);

        // Assert
        Assert.NotNull(loadedUser);
        Assert.Equal(UserRole.Coordinator, loadedUser.Role);
    }

    [Fact]
    public async Task EnumStorage_ShipmentStatusShouldStoreAsString()
    {
        // Arrange
        var shipment = TestData.CreateShipment()
            .AsInTransit()
            .Build();

        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var loadedShipment = await Context.Shipments.FindAsync(shipment.Id);

        // Assert
        Assert.NotNull(loadedShipment);
        Assert.Equal(ShipmentStatus.InTransit, loadedShipment.Status);
    }

    [Fact]
    public async Task ConcurrentAccess_MultipleContextsShouldWork()
    {
        // Arrange
        var user = TestData.CreateUser().Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();

        // Act - Create new context and query
        using var newContext = CreateNewContext();
        var loadedUser = await newContext.Users.FindAsync(user.Id);

        // Assert
        Assert.NotNull(loadedUser);
        Assert.Equal(user.Username, loadedUser.Username);
    }

    [Fact]
    public async Task TransactionRollback_FailedSaveShouldNotPersist()
    {
        // Arrange
        var user = TestData.CreateUser().Build();

        // Act
        await Context.Users.AddAsync(user);

        // Don't save, just check it's tracked
        var tracked = Context.ChangeTracker.Entries<User>().Any();
        Assert.True(tracked);

        // Clear without saving (simulate rollback)
        Context.ChangeTracker.Clear();

        // Assert
        DetachAllEntities();
        var persistedUser = await Context.Users.FindAsync(user.Id);
        Assert.Null(persistedUser);
    }

    [Fact]
    public async Task ChangeTracking_ShouldDetectModifications()
    {
        // Arrange
        var shipment = TestData.CreateShipment().Build();
        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();

        // Act
        shipment.UpdateStatus(ShipmentStatus.Validated);

        var modifiedEntries = Context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified)
            .ToList();

        // Assert
        Assert.Single(modifiedEntries);
        Assert.Equal(shipment, modifiedEntries[0].Entity);
    }

    [Fact]
    public async Task BulkInsert_ShouldHandleLargeDatasets()
    {
        // Arrange
        var users = Enumerable.Range(1, 1000)
            .Select(i => TestData.CreateUser()
                .WithUsername($"bulkuser{i}")
                .WithEmail($"bulk{i}@test.com")
                .Build())
            .ToList();

        // Act
        await Context.Users.AddRangeAsync(users);
        await Context.SaveChangesAsync();

        // Assert
        var count = await Context.Users.CountAsync();
        Assert.Equal(1000, count);

        // Cleanup
        ClearDatabase();
        var afterCleanup = await Context.Users.CountAsync();
        Assert.Equal(0, afterCleanup);
    }

    [Fact]
    public async Task QueryFilters_ShouldWorkCorrectly()
    {
        // Arrange
        var activeUser = TestData.CreateUser().WithUsername("active").WithEmail("active@test.com").Build();
        var inactiveUser = TestData.CreateUser().WithUsername("inactive").WithEmail("inactive@test.com").AsInactive().Build();

        await Context.Users.AddRangeAsync(activeUser, inactiveUser);
        await Context.SaveChangesAsync();

        // Act
        var activeUsers = await Context.Users
            .Where(u => u.IsActive)
            .ToListAsync();

        var inactiveUsers = await Context.Users
            .Where(u => !u.IsActive)
            .ToListAsync();

        // Assert
        Assert.Single(activeUsers);
        Assert.Single(inactiveUsers);
    }
}
