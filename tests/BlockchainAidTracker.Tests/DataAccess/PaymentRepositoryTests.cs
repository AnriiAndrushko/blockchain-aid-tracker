using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Tests.Infrastructure;
using Xunit;

namespace BlockchainAidTracker.Tests.DataAccess;

public class PaymentRepositoryTests : DatabaseTestBase
{
    private readonly PaymentRepository _repository;

    public PaymentRepositoryTests()
    {
        _repository = new PaymentRepository(Context);
    }

    #region GetBySupplierIdAsync Tests

    [Fact]
    public async Task GetBySupplierIdAsync_SupplierWithPayments_ShouldReturnAll()
    {
        // Arrange
        var supplierId = "supplier-123";
        var payment1 = TestData.CreatePaymentRecord()
            .WithSupplierId(supplierId)
            .WithAmount(1000m)
            .Build();
        var payment2 = TestData.CreatePaymentRecord()
            .WithSupplierId(supplierId)
            .WithAmount(2000m)
            .Build();

        await Context.PaymentRecords.AddRangeAsync(payment1, payment2);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _repository.GetBySupplierIdAsync(supplierId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal(supplierId, p.SupplierId));
    }

    #endregion

    #region GetByShipmentIdAsync Tests

    [Fact]
    public async Task GetByShipmentIdAsync_ShipmentWithPayments_ShouldReturnAll()
    {
        // Arrange
        var shipmentId = "shipment-123";
        var payment1 = TestData.CreatePaymentRecord().Build();
        payment1.ShipmentId = shipmentId;
        var payment2 = TestData.CreatePaymentRecord().Build();
        payment2.ShipmentId = shipmentId;

        await Context.PaymentRecords.AddRangeAsync(payment1, payment2);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _repository.GetByShipmentIdAsync(shipmentId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal(shipmentId, p.ShipmentId));
    }

    #endregion

    #region GetPendingAsync Tests

    [Fact]
    public async Task GetPendingAsync_WithInitiatedPayments_ShouldReturnPending()
    {
        // Arrange
        var pending = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Initiated)
            .Build();
        var completed = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Completed)
            .Build();

        await Context.PaymentRecords.AddRangeAsync(pending, completed);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _repository.GetPendingAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(PaymentRecordStatus.Initiated, result[0].Status);
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_WithMixedStatuses_ShouldReturnOnlySpecificStatus()
    {
        // Arrange
        var completed1 = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Completed)
            .Build();
        var completed2 = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Completed)
            .Build();
        var failed = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Failed)
            .Build();

        await Context.PaymentRecords.AddRangeAsync(completed1, completed2, failed);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _repository.GetByStatusAsync(PaymentRecordStatus.Completed);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal(PaymentRecordStatus.Completed, p.Status));
    }

    #endregion

    #region GetRetryableFailedPaymentsAsync Tests

    [Fact]
    public async Task GetRetryableFailedPaymentsAsync_WithFailedAndRetryablePayments_ShouldReturnRetryable()
    {
        // Arrange
        var retryable = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Failed)
            .WithAttemptCount(1)
            .Build();
        var exhausted = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Failed)
            .WithAttemptCount(3)
            .Build();

        await Context.PaymentRecords.AddRangeAsync(retryable, exhausted);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _repository.GetRetryableFailedPaymentsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(PaymentRecordStatus.Failed, result[0].Status);
        Assert.True(result[0].AttemptCount < 3);
    }

    #endregion

    #region GetBySupplierAndStatusAsync Tests

    [Fact]
    public async Task GetBySupplierAndStatusAsync_WithMixedData_ShouldReturnFiltered()
    {
        // Arrange
        var supplierId = "supplier-123";
        var completed = TestData.CreatePaymentRecord()
            .WithSupplierId(supplierId)
            .WithStatus(PaymentRecordStatus.Completed)
            .Build();
        var failed = TestData.CreatePaymentRecord()
            .WithSupplierId(supplierId)
            .WithStatus(PaymentRecordStatus.Failed)
            .Build();
        var otherSupplier = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Completed)
            .Build();

        await Context.PaymentRecords.AddRangeAsync(completed, failed, otherSupplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _repository.GetBySupplierAndStatusAsync(supplierId, PaymentRecordStatus.Completed);

        // Assert
        Assert.Single(result);
        Assert.Equal(supplierId, result[0].SupplierId);
        Assert.Equal(PaymentRecordStatus.Completed, result[0].Status);
    }

    #endregion

    #region GetTotalBySupplierAndStatusAsync Tests

    [Fact]
    public async Task GetTotalBySupplierAndStatusAsync_WithMultiplePayments_ShouldReturnSum()
    {
        // Arrange
        var supplierId = "supplier-123";
        var payment1 = TestData.CreatePaymentRecord()
            .WithSupplierId(supplierId)
            .WithAmount(1000m)
            .WithStatus(PaymentRecordStatus.Completed)
            .Build();
        var payment2 = TestData.CreatePaymentRecord()
            .WithSupplierId(supplierId)
            .WithAmount(2000m)
            .WithStatus(PaymentRecordStatus.Completed)
            .Build();
        var failed = TestData.CreatePaymentRecord()
            .WithSupplierId(supplierId)
            .WithAmount(500m)
            .WithStatus(PaymentRecordStatus.Failed)
            .Build();

        await Context.PaymentRecords.AddRangeAsync(payment1, payment2, failed);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTotalBySupplierAndStatusAsync(supplierId, PaymentRecordStatus.Completed);

        // Assert
        Assert.Equal(3000m, result);
    }

    [Fact]
    public async Task GetTotalBySupplierAndStatusAsync_NoPayments_ShouldReturnZero()
    {
        // Act
        var result = await _repository.GetTotalBySupplierAndStatusAsync("supplier-no-payments", PaymentRecordStatus.Completed);

        // Assert
        Assert.Equal(0m, result);
    }

    #endregion

    #region GetByDateRangeAsync Tests

    [Fact]
    public async Task GetByDateRangeAsync_WithPaymentsInRange_ShouldReturnInRange()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var payment1 = TestData.CreatePaymentRecord().Build();
        // Override timestamp to be within range
        payment1.CreatedTimestamp = now.AddHours(-1);

        await Context.PaymentRecords.AddAsync(payment1);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _repository.GetByDateRangeAsync(now.AddHours(-2), now.AddHours(1));

        // Assert
        Assert.Single(result);
    }

    #endregion

    #region CRUD Tests

    [Fact]
    public async Task AddAsync_ValidPayment_ShouldAdd()
    {
        // Arrange
        var payment = TestData.CreatePaymentRecord().Build();

        // Act
        var result = await _repository.AddAsync(payment);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(payment.Id, result.Id);
    }

    [Fact]
    public async Task Update_ExistingPayment_ShouldUpdateStatus()
    {
        // Arrange
        var payment = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Initiated)
            .Build();
        await Context.PaymentRecords.AddAsync(payment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        payment.Status = PaymentRecordStatus.Completed;
        _repository.Update(payment);
        await Context.SaveChangesAsync();

        // Assert
        using var newContext = CreateNewContext();
        var updated = await newContext.PaymentRecords.FindAsync(payment.Id);
        Assert.NotNull(updated);
        Assert.Equal(PaymentRecordStatus.Completed, updated.Status);
    }

    #endregion
}
