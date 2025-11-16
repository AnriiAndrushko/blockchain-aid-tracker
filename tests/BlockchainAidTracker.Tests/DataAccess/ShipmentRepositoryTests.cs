using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Tests.Infrastructure;
using Xunit;

namespace BlockchainAidTracker.Tests.DataAccess;

public class ShipmentRepositoryTests : DatabaseTestBase
{
    private readonly ShipmentRepository _shipmentRepository;

    public ShipmentRepositoryTests()
    {
        _shipmentRepository = new ShipmentRepository(Context);
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ShouldAddShipmentWithItems()
    {
        // Arrange
        var shipment = TestData.CreateShipment()
            .WithOrigin("New York")
            .WithDestination("London")
            .WithMedicalSupplies()
            .Build();

        // Act
        var result = await _shipmentRepository.AddAsync(shipment);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);

        // Verify in database
        DetachAllEntities();
        var dbShipment = await Context.Shipments.FindAsync(shipment.Id);
        Assert.NotNull(dbShipment);
    }

    [Fact]
    public async Task AddAsync_ShouldSetTimestamps()
    {
        // Arrange
        var shipment = TestData.CreateShipment().Build();

        // Act
        await _shipmentRepository.AddAsync(shipment);

        // Assert
        Assert.NotEqual(default, shipment.CreatedTimestamp);
        Assert.NotEqual(default, shipment.UpdatedTimestamp);
    }

    #endregion

    #region GetByIdWithItemsAsync Tests

    [Fact]
    public async Task GetByIdWithItemsAsync_ExistingShipment_ShouldReturnWithItems()
    {
        // Arrange
        var shipment = TestData.CreateShipment()
            .WithMedicalSupplies()
            .Build();
        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByIdWithItemsAsync(shipment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(shipment.Id, result.Id);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetByIdWithItemsAsync_NonExistingShipment_ShouldReturnNull()
    {
        // Act
        var result = await _shipmentRepository.GetByIdWithItemsAsync("non-existing-id");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllWithItemsAsync Tests

    [Fact]
    public async Task GetAllWithItemsAsync_ShouldReturnAllShipmentsWithItems()
    {
        // Arrange
        var shipment1 = TestData.CreateShipment()
            .WithOrigin("NYC")
            .WithMedicalSupplies()
            .Build();

        var shipment2 = TestData.CreateShipment()
            .WithOrigin("LA")
            .WithItem("Water", 100, "liters", "Water", 50m)
            .Build();

        await Context.Shipments.AddRangeAsync(shipment1, shipment2);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetAllWithItemsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.NotEmpty(s.Items));
    }

    [Fact]
    public async Task GetAllWithItemsAsync_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Act
        var result = await _shipmentRepository.GetAllWithItemsAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnShipmentsWithSpecificStatus()
    {
        // Arrange
        var created1 = TestData.CreateShipment().Build();
        var created2 = TestData.CreateShipment().Build();
        var validated = TestData.CreateShipment().AsValidated().Build();
        var inTransit = TestData.CreateShipment().AsInTransit().Build();

        await Context.Shipments.AddRangeAsync(created1, created2, validated, inTransit);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var createdShipments = await _shipmentRepository.GetByStatusAsync(ShipmentStatus.Created);

        // Assert
        Assert.Equal(2, createdShipments.Count);
        Assert.All(createdShipments, s => Assert.Equal(ShipmentStatus.Created, s.Status));
    }

    [Fact]
    public async Task GetByStatusAsync_NoShipmentsWithStatus_ShouldReturnEmptyList()
    {
        // Act
        var result = await _shipmentRepository.GetByStatusAsync(ShipmentStatus.Confirmed);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnShipmentsOrderedByNewest()
    {
        // Arrange
        var old = TestData.CreateShipment().Build();
        await Context.Shipments.AddAsync(old);
        await Context.SaveChangesAsync();
        await Task.Delay(10); // Small delay to ensure different timestamps

        var recent = TestData.CreateShipment().Build();
        await Context.Shipments.AddAsync(recent);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var shipments = await _shipmentRepository.GetByStatusAsync(ShipmentStatus.Created);

        // Assert
        Assert.Equal(2, shipments.Count);
        Assert.True(shipments[0].CreatedTimestamp >= shipments[1].CreatedTimestamp);
    }

    #endregion

    #region GetByRecipientAsync Tests

    [Fact]
    public async Task GetByRecipientAsync_ShouldReturnShipmentsForRecipient()
    {
        // Arrange
        var recipientKey = "recipient-123";
        var shipment1 = TestData.CreateShipment().WithRecipient(recipientKey).Build();
        var shipment2 = TestData.CreateShipment().WithRecipient(recipientKey).Build();
        var shipment3 = TestData.CreateShipment().WithRecipient("other-recipient").Build();

        await Context.Shipments.AddRangeAsync(shipment1, shipment2, shipment3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByRecipientAsync(recipientKey);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(recipientKey, s.AssignedRecipient));
    }

    #endregion

    #region GetByCoordinatorAsync Tests

    [Fact]
    public async Task GetByCoordinatorAsync_ShouldReturnShipmentsForCoordinator()
    {
        // Arrange
        var coordinatorKey = "coordinator-456";
        var shipment1 = TestData.CreateShipment().WithCoordinator(coordinatorKey).Build();
        var shipment2 = TestData.CreateShipment().WithCoordinator(coordinatorKey).Build();
        var shipment3 = TestData.CreateShipment().WithCoordinator("other-coordinator").Build();

        await Context.Shipments.AddRangeAsync(shipment1, shipment2, shipment3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByCoordinatorAsync(coordinatorKey);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(coordinatorKey, s.CoordinatorPublicKey));
    }

    #endregion

    #region GetByDonorAsync Tests

    [Fact]
    public async Task GetByDonorAsync_ShouldReturnShipmentsForDonor()
    {
        // Arrange
        var donorKey = "donor-789";
        var shipment1 = TestData.CreateShipment().WithDonor(donorKey).Build();
        var shipment2 = TestData.CreateShipment().WithDonor(donorKey).Build();
        var shipment3 = TestData.CreateShipment().WithDonor("other-donor").Build();

        await Context.Shipments.AddRangeAsync(shipment1, shipment2, shipment3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByDonorAsync(donorKey);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(donorKey, s.DonorPublicKey));
    }

    [Fact]
    public async Task GetByDonorAsync_NullDonor_ShouldReturnEmpty()
    {
        // Arrange
        var shipment = TestData.CreateShipment().Build(); // No donor
        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();

        // Act
        var result = await _shipmentRepository.GetByDonorAsync("some-donor");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetByQrCodeAsync Tests

    [Fact]
    public async Task GetByQrCodeAsync_ExistingQrCode_ShouldReturnShipment()
    {
        // Arrange
        var shipment = TestData.CreateShipment().WithMedicalSupplies().Build();
        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByQrCodeAsync(shipment.QrCodeData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(shipment.Id, result.Id);
        Assert.Equal(shipment.QrCodeData, result.QrCodeData);
        Assert.Equal(2, result.Items.Count); // Should include items
    }

    [Fact]
    public async Task GetByQrCodeAsync_NonExistingQrCode_ShouldReturnNull()
    {
        // Act
        var result = await _shipmentRepository.GetByQrCodeAsync("SHIPMENT-NONEXISTENT-123");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByDateRangeAsync Tests

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnShipmentsInRange()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 12, 31);

        var inRange1 = TestData.CreateShipment().Build();
        inRange1.GetType().GetProperty("CreatedTimestamp")!.SetValue(inRange1, new DateTime(2025, 6, 15));

        var inRange2 = TestData.CreateShipment().Build();
        inRange2.GetType().GetProperty("CreatedTimestamp")!.SetValue(inRange2, new DateTime(2025, 9, 20));

        var outOfRange = TestData.CreateShipment().Build();
        outOfRange.GetType().GetProperty("CreatedTimestamp")!.SetValue(outOfRange, new DateTime(2024, 12, 31));

        await Context.Shipments.AddRangeAsync(inRange1, inRange2, outOfRange);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s =>
        {
            Assert.True(s.CreatedTimestamp >= startDate);
            Assert.True(s.CreatedTimestamp <= endDate);
        });
    }

    [Fact]
    public async Task GetByDateRangeAsync_NoShipmentsInRange_ShouldReturnEmpty()
    {
        // Arrange
        var shipment = TestData.CreateShipment().Build();
        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();

        // Act
        var result = await _shipmentRepository.GetByDateRangeAsync(
            new DateTime(2020, 1, 1),
            new DateTime(2020, 12, 31)
        );

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldUpdateShipmentStatus()
    {
        // Arrange
        var shipment = TestData.CreateShipment().Build();
        Context.Shipments.Add(shipment);
        Context.SaveChanges();

        var originalStatus = shipment.Status;

        // Act
        shipment.UpdateStatus(ShipmentStatus.Validated);
        _shipmentRepository.Update(shipment);

        // Assert
        DetachAllEntities();
        var updatedShipment = Context.Shipments.Find(shipment.Id);
        Assert.NotNull(updatedShipment);
        Assert.NotEqual(originalStatus, updatedShipment.Status);
        Assert.Equal(ShipmentStatus.Validated, updatedShipment.Status);
    }

    [Fact]
    public void Update_ShouldUpdateTimestamp()
    {
        // Arrange
        var shipment = TestData.CreateShipment().Build();
        Context.Shipments.Add(shipment);
        Context.SaveChanges();

        var originalTimestamp = shipment.UpdatedTimestamp;
        System.Threading.Thread.Sleep(10); // Ensure time passes

        // Act
        shipment.UpdateStatus(ShipmentStatus.InTransit);
        _shipmentRepository.Update(shipment);

        // Assert
        Assert.True(shipment.UpdatedTimestamp > originalTimestamp);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void Remove_ShouldRemoveShipmentAndItems()
    {
        // Arrange
        var shipment = TestData.CreateShipment()
            .WithMedicalSupplies()
            .Build();
        Context.Shipments.Add(shipment);
        Context.SaveChanges();

        var shipmentId = shipment.Id;
        var itemCount = shipment.Items.Count;

        // Act
        _shipmentRepository.Remove(shipment);

        // Assert
        DetachAllEntities();
        var removedShipment = Context.Shipments.Find(shipmentId);
        Assert.Null(removedShipment);

        // Items should also be removed (cascade delete)
        var remainingItems = Context.ShipmentItems.Where(si => si.Id == shipment.Items.First().Id).ToList();
        Assert.Empty(remainingItems);
    }

    #endregion

    #region Complex Query Tests

    [Fact]
    public async Task ComplexQuery_FilterByMultipleCriteria()
    {
        // Arrange
        var recipientKey = "test-recipient";
        var shipment1 = TestData.CreateShipment()
            .WithRecipient(recipientKey)
            .WithStatus(ShipmentStatus.InTransit)
            .Build();

        var shipment2 = TestData.CreateShipment()
            .WithRecipient(recipientKey)
            .WithStatus(ShipmentStatus.Delivered)
            .Build();

        var shipment3 = TestData.CreateShipment()
            .WithRecipient("other-recipient")
            .WithStatus(ShipmentStatus.InTransit)
            .Build();

        await Context.Shipments.AddRangeAsync(shipment1, shipment2, shipment3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.FindAsync(s =>
            s.AssignedRecipient == recipientKey &&
            s.Status == ShipmentStatus.InTransit
        );

        // Assert
        Assert.Single(result);
        Assert.Equal(shipment1.Id, result[0].Id);
    }

    #endregion

    #region Database Cleanup Tests

    [Fact]
    public async Task CascadeDelete_RemovingShipmentShouldRemoveItems()
    {
        // Arrange
        var shipment = TestData.CreateShipment()
            .WithItem("Item1", 10, "units", "Category1")
            .WithItem("Item2", 20, "units", "Category2")
            .WithItem("Item3", 30, "units", "Category3")
            .Build();

        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();

        var itemIds = shipment.Items.Select(i => i.Id).ToList();
        Assert.Equal(3, itemIds.Count);

        // Act
        _shipmentRepository.Remove(shipment);

        // Assert
        DetachAllEntities();
        var remainingItems = Context.ShipmentItems
            .Where(si => itemIds.Contains(si.Id))
            .ToList();

        Assert.Empty(remainingItems);
    }

    [Fact]
    public async Task BulkOperations_AddMultipleShipmentsAndCleanup()
    {
        // Arrange
        var shipments = Enumerable.Range(1, 20)
            .Select(i => TestData.CreateShipment()
                .WithOrigin($"Origin {i}")
                .WithDestination($"Destination {i}")
                .WithItem($"Item {i}", i * 10, "units", "Test")
                .Build())
            .ToList();

        // Act
        await _shipmentRepository.AddRangeAsync(shipments);

        // Assert
        var allShipments = await _shipmentRepository.GetAllWithItemsAsync();
        Assert.Equal(20, allShipments.Count);
        Assert.All(allShipments, s => Assert.Single(s.Items));

        // Cleanup test
        ClearDatabase();
        var shipmentsAfterCleanup = await _shipmentRepository.GetAllAsync();
        Assert.Empty(shipmentsAfterCleanup);
    }

    #endregion

    #region Donor and Logistics Partner Query Tests

    [Fact]
    public async Task GetByDonorIdAsync_ReturnsDonorShipments()
    {
        // Arrange
        var donorId = "donor-123";
        var shipment1 = TestData.CreateShipment()
            .WithOrigin("Origin1")
            .WithDestination("Destination1")
            .Build();
        shipment1.DonorId = donorId;

        var shipment2 = TestData.CreateShipment()
            .WithOrigin("Origin2")
            .WithDestination("Destination2")
            .Build();
        shipment2.DonorId = donorId;

        var shipment3 = TestData.CreateShipment()
            .WithOrigin("Origin3")
            .WithDestination("Destination3")
            .Build();
        shipment3.DonorId = "other-donor";

        await Context.Shipments.AddRangeAsync(shipment1, shipment2, shipment3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByDonorIdAsync(donorId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(donorId, s.DonorId));
        Assert.Contains(result, s => s.Id == shipment1.Id);
        Assert.Contains(result, s => s.Id == shipment2.Id);
    }

    [Fact]
    public async Task GetByDonorIdAsync_ReturnsEmptyForNonexistentDonor()
    {
        // Arrange
        var shipment = TestData.CreateShipment().Build();
        shipment.DonorId = "donor-123";

        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByDonorIdAsync("nonexistent-donor");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByDonorIdAsync_IncludesItems()
    {
        // Arrange
        var donorId = "donor-456";
        var shipment = TestData.CreateShipment()
            .WithItem("Item1", 10, "kg", "Food")
            .WithItem("Item2", 20, "boxes", "Medical")
            .Build();
        shipment.DonorId = donorId;

        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByDonorIdAsync(donorId);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Items.Count);
    }

    [Fact]
    public async Task GetByLogisticsPartnerIdAsync_ReturnsAssignedShipments()
    {
        // Arrange
        var logisticsPartnerId = "lp-123";
        var shipment1 = TestData.CreateShipment()
            .WithOrigin("Origin1")
            .WithDestination("Destination1")
            .Build();
        shipment1.AssignedLogisticsPartnerId = logisticsPartnerId;

        var shipment2 = TestData.CreateShipment()
            .WithOrigin("Origin2")
            .WithDestination("Destination2")
            .Build();
        shipment2.AssignedLogisticsPartnerId = logisticsPartnerId;

        var shipment3 = TestData.CreateShipment()
            .WithOrigin("Origin3")
            .WithDestination("Destination3")
            .Build();
        shipment3.AssignedLogisticsPartnerId = "other-lp";

        await Context.Shipments.AddRangeAsync(shipment1, shipment2, shipment3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByLogisticsPartnerIdAsync(logisticsPartnerId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(logisticsPartnerId, s.AssignedLogisticsPartnerId));
        Assert.Contains(result, s => s.Id == shipment1.Id);
        Assert.Contains(result, s => s.Id == shipment2.Id);
    }

    [Fact]
    public async Task GetByLogisticsPartnerIdAsync_ReturnsEmptyForNonexistentPartner()
    {
        // Arrange
        var shipment = TestData.CreateShipment().Build();
        shipment.AssignedLogisticsPartnerId = "lp-123";

        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByLogisticsPartnerIdAsync("nonexistent-lp");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByLogisticsPartnerIdAsync_IncludesItems()
    {
        // Arrange
        var logisticsPartnerId = "lp-456";
        var shipment = TestData.CreateShipment()
            .WithItem("Item1", 10, "kg", "Food")
            .WithItem("Item2", 20, "boxes", "Medical")
            .WithItem("Item3", 30, "units", "Supplies")
            .Build();
        shipment.AssignedLogisticsPartnerId = logisticsPartnerId;

        await Context.Shipments.AddAsync(shipment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByLogisticsPartnerIdAsync(logisticsPartnerId);

        // Assert
        Assert.Single(result);
        Assert.Equal(3, result[0].Items.Count);
    }

    [Fact]
    public async Task GetByDonorIdAsync_OrdersByCreatedTimestampDescending()
    {
        // Arrange
        var donorId = "donor-789";
        var shipment1 = TestData.CreateShipment().Build();
        shipment1.DonorId = donorId;
        shipment1.CreatedTimestamp = DateTime.UtcNow.AddDays(-2);

        var shipment2 = TestData.CreateShipment().Build();
        shipment2.DonorId = donorId;
        shipment2.CreatedTimestamp = DateTime.UtcNow.AddDays(-1);

        var shipment3 = TestData.CreateShipment().Build();
        shipment3.DonorId = donorId;
        shipment3.CreatedTimestamp = DateTime.UtcNow;

        await Context.Shipments.AddRangeAsync(shipment1, shipment2, shipment3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByDonorIdAsync(donorId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(shipment3.Id, result[0].Id); // Most recent first
        Assert.Equal(shipment2.Id, result[1].Id);
        Assert.Equal(shipment1.Id, result[2].Id);
    }

    [Fact]
    public async Task GetByLogisticsPartnerIdAsync_OrdersByCreatedTimestampDescending()
    {
        // Arrange
        var logisticsPartnerId = "lp-789";
        var shipment1 = TestData.CreateShipment().Build();
        shipment1.AssignedLogisticsPartnerId = logisticsPartnerId;
        shipment1.CreatedTimestamp = DateTime.UtcNow.AddDays(-3);

        var shipment2 = TestData.CreateShipment().Build();
        shipment2.AssignedLogisticsPartnerId = logisticsPartnerId;
        shipment2.CreatedTimestamp = DateTime.UtcNow.AddDays(-1);

        var shipment3 = TestData.CreateShipment().Build();
        shipment3.AssignedLogisticsPartnerId = logisticsPartnerId;
        shipment3.CreatedTimestamp = DateTime.UtcNow;

        await Context.Shipments.AddRangeAsync(shipment1, shipment2, shipment3);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _shipmentRepository.GetByLogisticsPartnerIdAsync(logisticsPartnerId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(shipment3.Id, result[0].Id); // Most recent first
        Assert.Equal(shipment2.Id, result[1].Id);
        Assert.Equal(shipment1.Id, result[2].Id);
    }

    #endregion
}
