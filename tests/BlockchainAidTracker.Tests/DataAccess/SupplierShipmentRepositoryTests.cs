using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Tests.Infrastructure;
using Xunit;

namespace BlockchainAidTracker.Tests.DataAccess;

public class SupplierShipmentRepositoryTests : DatabaseTestBase
{
    private readonly SupplierShipmentRepository _repository;

    public SupplierShipmentRepositoryTests()
    {
        _repository = new SupplierShipmentRepository(Context);
    }

    #region GetByShipmentIdAsync Tests

    [Fact]
    public async Task GetByShipmentIdAsync_MultipleSupplierShipments_ShouldReturnAllForShipment()
    {
        // Arrange
        var shipmentId = "shipment-123";
        var shipment = TestData.CreateShipment()
            .WithId(shipmentId)
            .Build();
        var supplier1 = TestData.CreateSupplier().Build();
        var supplier2 = TestData.CreateSupplier().Build();
        var ss1 = TestData.CreateSupplierShipment()
            .WithShipmentId(shipmentId)
            .WithSupplierId(supplier1.Id)
            .WithValue(1000m)
            .Build();
        var ss2 = TestData.CreateSupplierShipment()
            .WithShipmentId(shipmentId)
            .WithSupplierId(supplier2.Id)
            .WithValue(2000m)
            .Build();

        await Context.Shipments.AddAsync(shipment);
        await Context.Suppliers.AddRangeAsync(supplier1, supplier2);
        await Context.SupplierShipments.AddRangeAsync(ss1, ss2);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _repository.GetByShipmentIdAsync(shipmentId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, ss => Assert.Equal(shipmentId, ss.ShipmentId));
    }

    [Fact]
    public async Task GetByShipmentIdAsync_NoShipments_ShouldReturnEmpty()
    {
        // Act
        var result = await _repository.GetByShipmentIdAsync("non-existing-shipment");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetBySupplierIdAsync Tests

    [Fact]
    public async Task GetBySupplierIdAsync_SupplierWithShipments_ShouldReturnAll()
    {
        // Arrange
        var supplierId = "supplier-123";
        var supplier = TestData.CreateSupplier()
            .WithId(supplierId)
            .Build();
        var shipment1 = TestData.CreateShipment().Build();
        var shipment2 = TestData.CreateShipment().Build();
        var ss1 = TestData.CreateSupplierShipment()
            .WithSupplierId(supplierId)
            .WithShipmentId(shipment1.Id)
            .Build();
        var ss2 = TestData.CreateSupplierShipment()
            .WithSupplierId(supplierId)
            .WithShipmentId(shipment2.Id)
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.Shipments.AddRangeAsync(shipment1, shipment2);
        await Context.SupplierShipments.AddRangeAsync(ss1, ss2);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _repository.GetBySupplierIdAsync(supplierId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, ss => Assert.Equal(supplierId, ss.SupplierId));
    }

    #endregion

    #region GetPendingPaymentsAsync Tests

    [Fact]
    public async Task GetPendingPaymentsAsync_WithPendingPayments_ShouldReturnPending()
    {
        // Arrange
        var supplierId = "supplier-123";
        var supplier = TestData.CreateSupplier()
            .WithId(supplierId)
            .Build();
        var shipment = TestData.CreateShipment().Build();
        var pending = TestData.CreateSupplierShipment()
            .WithSupplierId(supplierId)
            .WithShipmentId(shipment.Id)
            .WithPaymentStatus(SupplierShipmentPaymentStatus.Pending)
            .Build();
        var completed = TestData.CreateSupplierShipment()
            .WithSupplierId(supplierId)
            .WithShipmentId(shipment.Id)
            .WithPaymentStatus(SupplierShipmentPaymentStatus.Completed)
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.Shipments.AddAsync(shipment);
        await Context.SupplierShipments.AddRangeAsync(pending, completed);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _repository.GetPendingPaymentsAsync(supplierId);

        // Assert
        Assert.Single(result);
        Assert.Equal(SupplierShipmentPaymentStatus.Pending, result[0].PaymentStatus);
    }

    #endregion

    #region GetByPaymentStatusAsync Tests

    [Fact]
    public async Task GetByPaymentStatusAsync_WithMixedStatuses_ShouldReturnOnlySpecificStatus()
    {
        // Arrange
        var supplierId = "supplier-123";
        var supplier = TestData.CreateSupplier()
            .WithId(supplierId)
            .Build();
        var shipment = TestData.CreateShipment().Build();
        var completed1 = TestData.CreateSupplierShipment()
            .WithSupplierId(supplierId)
            .WithShipmentId(shipment.Id)
            .WithPaymentStatus(SupplierShipmentPaymentStatus.Completed)
            .Build();
        var completed2 = TestData.CreateSupplierShipment()
            .WithSupplierId(supplierId)
            .WithShipmentId(shipment.Id)
            .WithPaymentStatus(SupplierShipmentPaymentStatus.Completed)
            .Build();
        var failed = TestData.CreateSupplierShipment()
            .WithSupplierId(supplierId)
            .WithShipmentId(shipment.Id)
            .WithPaymentStatus(SupplierShipmentPaymentStatus.Failed)
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.Shipments.AddAsync(shipment);
        await Context.SupplierShipments.AddRangeAsync(completed1, completed2, failed);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _repository.GetByPaymentStatusAsync(supplierId, SupplierShipmentPaymentStatus.Completed);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, ss => Assert.Equal(SupplierShipmentPaymentStatus.Completed, ss.PaymentStatus));
    }

    #endregion

    #region GetTotalPendingPaymentValueAsync Tests

    [Fact]
    public async Task GetTotalPendingPaymentValueAsync_WithPendingPayments_ShouldReturnTotalValue()
    {
        // Arrange
        var supplierId = "supplier-123";
        var supplier = TestData.CreateSupplier()
            .WithId(supplierId)
            .Build();
        var shipment = TestData.CreateShipment().Build();
        var ss1 = TestData.CreateSupplierShipment()
            .WithSupplierId(supplierId)
            .WithShipmentId(shipment.Id)
            .WithValue(1000m)
            .WithPaymentStatus(SupplierShipmentPaymentStatus.Pending)
            .Build();
        var ss2 = TestData.CreateSupplierShipment()
            .WithSupplierId(supplierId)
            .WithShipmentId(shipment.Id)
            .WithValue(2000m)
            .WithPaymentStatus(SupplierShipmentPaymentStatus.Pending)
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.Shipments.AddAsync(shipment);
        await Context.SupplierShipments.AddRangeAsync(ss1, ss2);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTotalPendingPaymentValueAsync(supplierId);

        // Assert
        Assert.Equal(3000m, result);
    }

    [Fact]
    public async Task GetTotalPendingPaymentValueAsync_NoPayments_ShouldReturnZero()
    {
        // Act
        var result = await _repository.GetTotalPendingPaymentValueAsync("supplier-with-no-payments");

        // Assert
        Assert.Equal(0m, result);
    }

    #endregion

    #region ReleasePaymentAsync Tests

    [Fact]
    public async Task ReleasePaymentAsync_ExistingRecord_ShouldMarkAsReleased()
    {
        // Arrange
        var ss = TestData.CreateSupplierShipment()
            .WithPaymentStatus(SupplierShipmentPaymentStatus.Pending)
            .Build();
        await Context.SupplierShipments.AddAsync(ss);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _repository.ReleasePaymentAsync(ss.Id, "tx-hash-123");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PaymentReleased);
        Assert.Equal("tx-hash-123", result.PaymentTransactionReference);
        Assert.Equal(SupplierShipmentPaymentStatus.Completed, result.PaymentStatus);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidSupplierShipment_ShouldAdd()
    {
        // Arrange
        var ss = TestData.CreateSupplierShipment().Build();

        // Act
        var result = await _repository.AddAsync(ss);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ss.Id, result.Id);
    }

    #endregion
}
