using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;
using BlockchainAidTracker.Services.Services;
using BlockchainAidTracker.Tests.Infrastructure;
using Xunit;

namespace BlockchainAidTracker.Tests.Services;

public class PaymentServiceTests : DatabaseTestBase
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly ISupplierShipmentRepository _supplierShipmentRepository;

    public PaymentServiceTests()
    {
        _paymentRepository = new PaymentRepository(Context);
        _supplierRepository = new SupplierRepository(Context);
        _supplierShipmentRepository = new SupplierShipmentRepository(Context);
        _paymentService = new PaymentService(_paymentRepository, _supplierRepository, _supplierShipmentRepository);
    }

    #region CalculatePaymentAmountAsync Tests

    [Fact]
    public async Task CalculatePaymentAmountAsync_WithPendingPayment_ShouldReturnAmount()
    {
        // Arrange
        var supplierId = "supplier-123";
        var shipmentId = "shipment-123";
        var supplier = TestData.CreateSupplier()
            .WithId(supplierId)
            .Build();
        var shipment = TestData.CreateShipment()
            .WithId(shipmentId)
            .Build();
        var ss = TestData.CreateSupplierShipment()
            .WithSupplierId(supplierId)
            .WithShipmentId(shipmentId)
            .WithValue(5000m)
            .WithPaymentStatus(SupplierShipmentPaymentStatus.Pending)
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.Shipments.AddAsync(shipment);
        await Context.SupplierShipments.AddAsync(ss);
        await Context.SaveChangesAsync();

        // Act
        var result = await _paymentService.CalculatePaymentAmountAsync(shipmentId, supplierId);

        // Assert
        Assert.Equal(5000m, result);
    }

    [Fact]
    public async Task CalculatePaymentAmountAsync_NoPayments_ShouldReturnZero()
    {
        // Act
        var result = await _paymentService.CalculatePaymentAmountAsync("shipment-123", "supplier-123");

        // Assert
        Assert.Equal(0m, result);
    }

    #endregion

    #region InitiatePaymentAsync Tests

    [Fact]
    public async Task InitiatePaymentAsync_VerifiedSupplier_ShouldCreatePayment()
    {
        // Arrange
        var supplier = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Verified)
            .WithPaymentThreshold(1000m)
            .Build();
        var ss = TestData.CreateSupplierShipment()
            .WithSupplierId(supplier.Id)
            .WithValue(5000m)
            .WithPaymentStatus(SupplierShipmentPaymentStatus.Pending)
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.SupplierShipments.AddAsync(ss);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _paymentService.InitiatePaymentAsync(ss.ShipmentId, supplier.Id, PaymentMethod.BankTransfer);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5000m, result.Amount);
        Assert.Equal("Initiated", result.Status);
    }

    [Fact]
    public async Task InitiatePaymentAsync_UnverifiedSupplier_ShouldThrowException()
    {
        // Arrange
        var supplier = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Pending)
            .Build();
        var ss = TestData.CreateSupplierShipment()
            .WithSupplierId(supplier.Id)
            .WithValue(5000m)
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.SupplierShipments.AddAsync(ss);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() =>
            _paymentService.InitiatePaymentAsync(ss.ShipmentId, supplier.Id, PaymentMethod.BankTransfer));
    }

    [Fact]
    public async Task InitiatePaymentAsync_BelowThreshold_ShouldThrowException()
    {
        // Arrange
        var supplier = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Verified)
            .WithPaymentThreshold(5000m)
            .Build();
        var ss = TestData.CreateSupplierShipment()
            .WithSupplierId(supplier.Id)
            .WithValue(1000m) // Below threshold
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.SupplierShipments.AddAsync(ss);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() =>
            _paymentService.InitiatePaymentAsync(ss.ShipmentId, supplier.Id, PaymentMethod.BankTransfer));
    }

    #endregion

    #region CompletePaymentAsync Tests

    [Fact]
    public async Task CompletePaymentAsync_InitiatedPayment_ShouldMarkComplete()
    {
        // Arrange
        var payment = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Initiated)
            .Build();

        await Context.PaymentRecords.AddAsync(payment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _paymentService.CompletePaymentAsync(payment.Id, "external-ref-123", "tx-hash-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
        Assert.Equal("external-ref-123", result.ExternalPaymentReference);
    }

    #endregion

    #region FailPaymentAsync Tests

    [Fact]
    public async Task FailPaymentAsync_PaymentRecord_ShouldMarkFailed()
    {
        // Arrange
        var payment = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Initiated)
            .Build();

        await Context.PaymentRecords.AddAsync(payment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _paymentService.FailPaymentAsync(payment.Id, "Bank transfer failed");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Failed", result.Status);
        Assert.Equal("Bank transfer failed", result.FailureReason);
    }

    #endregion

    #region RetryPaymentAsync Tests

    [Fact]
    public async Task RetryPaymentAsync_FailedPayment_ShouldMarkRetrying()
    {
        // Arrange
        var payment = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Failed)
            .WithAttemptCount(1)
            .Build();

        await Context.PaymentRecords.AddAsync(payment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _paymentService.RetryPaymentAsync(payment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Retrying", result.Status);
        Assert.Equal(2, result.AttemptCount);
    }

    [Fact]
    public async Task RetryPaymentAsync_ExhaustedAttempts_ShouldThrowException()
    {
        // Arrange
        var payment = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Failed)
            .WithAttemptCount(3)
            .Build();

        await Context.PaymentRecords.AddAsync(payment);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() =>
            _paymentService.RetryPaymentAsync(payment.Id));
    }

    #endregion

    #region GetPendingPaymentsAsync Tests

    [Fact]
    public async Task GetPendingPaymentsAsync_WithInitiatedPayments_ShouldReturnAll()
    {
        // Arrange
        var payment1 = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Initiated)
            .Build();
        var payment2 = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Initiated)
            .Build();

        await Context.PaymentRecords.AddRangeAsync(payment1, payment2);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _paymentService.GetPendingPaymentsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal("Initiated", p.Status));
    }

    #endregion

    #region GetRetryablePaymentsAsync Tests

    [Fact]
    public async Task GetRetryablePaymentsAsync_WithRetryablePayments_ShouldReturn()
    {
        // Arrange
        var retryable = TestData.CreatePaymentRecord()
            .WithStatus(PaymentRecordStatus.Failed)
            .WithAttemptCount(1)
            .Build();

        await Context.PaymentRecords.AddAsync(retryable);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _paymentService.GetRetryablePaymentsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Failed", result[0].Status);
    }

    #endregion

    #region GetSupplierPaymentsAsync Tests

    [Fact]
    public async Task GetSupplierPaymentsAsync_ExistingSupplier_ShouldReturnPayments()
    {
        // Arrange
        var supplier = TestData.CreateSupplier().Build();
        var payment1 = TestData.CreatePaymentRecord().WithSupplierId(supplier.Id).Build();
        var payment2 = TestData.CreatePaymentRecord().WithSupplierId(supplier.Id).Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.PaymentRecords.AddRangeAsync(payment1, payment2);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _paymentService.GetSupplierPaymentsAsync(supplier.Id);

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region IsSupplierEligibleForPaymentAsync Tests

    [Fact]
    public async Task IsSupplierEligibleForPaymentAsync_VerifiedAndActive_ShouldReturnTrue()
    {
        // Arrange
        var supplier = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Verified)
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _paymentService.IsSupplierEligibleForPaymentAsync(supplier.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsSupplierEligibleForPaymentAsync_NotVerified_ShouldReturnFalse()
    {
        // Arrange
        var supplier = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Pending)
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _paymentService.IsSupplierEligibleForPaymentAsync(supplier.Id);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetSupplierTotalEarnedAsync Tests

    [Fact]
    public async Task GetSupplierTotalEarnedAsync_WithCompletedPayments_ShouldReturnTotal()
    {
        // Arrange
        var supplier = TestData.CreateSupplier().Build();
        var payment1 = TestData.CreatePaymentRecord()
            .WithSupplierId(supplier.Id)
            .WithAmount(1000m)
            .WithStatus(PaymentRecordStatus.Completed)
            .Build();
        var payment2 = TestData.CreatePaymentRecord()
            .WithSupplierId(supplier.Id)
            .WithAmount(2000m)
            .WithStatus(PaymentRecordStatus.Completed)
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.PaymentRecords.AddRangeAsync(payment1, payment2);
        await Context.SaveChangesAsync();

        // Act
        var result = await _paymentService.GetSupplierTotalEarnedAsync(supplier.Id);

        // Assert
        Assert.Equal(3000m, result);
    }

    #endregion
}
