using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.Supplier;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;
using BlockchainAidTracker.Services.Services;
using BlockchainAidTracker.Tests.Infrastructure;
using Moq;
using Xunit;

namespace BlockchainAidTracker.Tests.Services;

public class SupplierServiceTests : DatabaseTestBase
{
    private readonly ISupplierService _supplierService;
    private readonly ISupplierRepository _supplierRepository;
    private readonly ISupplierShipmentRepository _supplierShipmentRepository;
    private readonly IPaymentRepository _paymentRepository;

    public SupplierServiceTests()
    {
        _supplierRepository = new SupplierRepository(Context);
        _supplierShipmentRepository = new SupplierShipmentRepository(Context);
        _paymentRepository = new PaymentRepository(Context);
        _supplierService = new SupplierService(_supplierRepository, _supplierShipmentRepository, _paymentRepository);
    }

    #region RegisterSupplierAsync Tests

    [Fact]
    public async Task RegisterSupplierAsync_ValidRequest_ShouldCreateSupplier()
    {
        // Arrange
        var userId = "user-123";
        var request = new CreateSupplierRequest
        {
            CompanyName = "NewCompany",
            RegistrationId = "REG-123",
            ContactEmail = "contact@company.com",
            ContactPhone = "+1-555-0100",
            BusinessCategory = "Medical",
            BankDetails = "IBAN123456789",
            PaymentThreshold = 1000m,
            TaxId = "TAX-123"
        };
        var keyManagementService = new Mock<IKeyManagementService>();

        // Act
        var result = await _supplierService.RegisterSupplierAsync(userId, request, keyManagementService.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NewCompany", result.CompanyName);
        Assert.Equal("Pending", result.VerificationStatus);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task RegisterSupplierAsync_DuplicateCompanyName_ShouldThrowException()
    {
        // Arrange
        var existingSupplier = TestData.CreateSupplier()
            .WithCompanyName("DuplicateCompany")
            .Build();
        await Context.Suppliers.AddAsync(existingSupplier);
        await Context.SaveChangesAsync();

        var userId = "user-123";
        var request = new CreateSupplierRequest
        {
            CompanyName = "DuplicateCompany",
            RegistrationId = "REG-123",
            ContactEmail = "contact@company.com",
            ContactPhone = "+1-555-0100",
            BusinessCategory = "Medical",
            BankDetails = "IBAN123456789",
            PaymentThreshold = 1000m,
            TaxId = "TAX-456"
        };
        var keyManagementService = new Mock<IKeyManagementService>();
        DetachAllEntities();

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() =>
            _supplierService.RegisterSupplierAsync(userId, request, keyManagementService.Object));
    }

    [Fact]
    public async Task RegisterSupplierAsync_DuplicateTaxId_ShouldThrowException()
    {
        // Arrange
        var existingSupplier = TestData.CreateSupplier().Build();
        await Context.Suppliers.AddAsync(existingSupplier);
        await Context.SaveChangesAsync();

        var userId = "user-123";
        var request = new CreateSupplierRequest
        {
            CompanyName = "NewCompany",
            RegistrationId = "REG-123",
            ContactEmail = "contact@company.com",
            ContactPhone = "+1-555-0100",
            BusinessCategory = "Medical",
            BankDetails = "IBAN123456789",
            PaymentThreshold = 1000m,
            TaxId = existingSupplier.TaxId
        };
        var keyManagementService = new Mock<IKeyManagementService>();
        DetachAllEntities();

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() =>
            _supplierService.RegisterSupplierAsync(userId, request, keyManagementService.Object));
    }

    #endregion

    #region GetSupplierByIdAsync Tests

    [Fact]
    public async Task GetSupplierByIdAsync_ExistingSupplier_ShouldReturnSupplier()
    {
        // Arrange
        var supplier = TestData.CreateSupplier().Build();
        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierService.GetSupplierByIdAsync(supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(supplier.CompanyName, result.CompanyName);
    }

    [Fact]
    public async Task GetSupplierByIdAsync_NonExistingSupplier_ShouldReturnNull()
    {
        // Act
        var result = await _supplierService.GetSupplierByIdAsync("non-existing-id");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region VerifySupplierAsync Tests

    [Fact]
    public async Task VerifySupplierAsync_PendingSupplier_ShouldChangeToVerified()
    {
        // Arrange
        var supplier = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Pending)
            .Build();
        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierService.VerifySupplierAsync(supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Verified", result.VerificationStatus);
    }

    [Fact]
    public async Task VerifySupplierAsync_NonExistingSupplier_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _supplierService.VerifySupplierAsync("non-existing-id"));
    }

    #endregion

    #region RejectSupplierAsync Tests

    [Fact]
    public async Task RejectSupplierAsync_PendingSupplier_ShouldChangeToRejected()
    {
        // Arrange
        var supplier = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Pending)
            .Build();
        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierService.RejectSupplierAsync(supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Rejected", result.VerificationStatus);
    }

    #endregion

    #region ActivateSupplierAsync Tests

    [Fact]
    public async Task ActivateSupplierAsync_InactiveSupplier_ShouldActivate()
    {
        // Arrange
        var supplier = TestData.CreateSupplier().AsInactive().Build();
        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierService.ActivateSupplierAsync(supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
    }

    #endregion

    #region DeactivateSupplierAsync Tests

    [Fact]
    public async Task DeactivateSupplierAsync_ActiveSupplier_ShouldDeactivate()
    {
        // Arrange
        var supplier = TestData.CreateSupplier().Build();
        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierService.DeactivateSupplierAsync(supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    #endregion

    #region GetSupplierShipmentsAsync Tests

    [Fact]
    public async Task GetSupplierShipmentsAsync_WithShipments_ShouldReturnShipments()
    {
        // Arrange
        var supplier = TestData.CreateSupplier().Build();
        var shipment1 = TestData.CreateShipment().Build();
        var shipment2 = TestData.CreateShipment().Build();
        var ss1 = TestData.CreateSupplierShipment().WithSupplierId(supplier.Id).WithShipmentId(shipment1.Id).Build();
        var ss2 = TestData.CreateSupplierShipment().WithSupplierId(supplier.Id).WithShipmentId(shipment2.Id).Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.Shipments.AddRangeAsync(shipment1, shipment2);
        await Context.SupplierShipments.AddRangeAsync(ss1, ss2);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierService.GetSupplierShipmentsAsync(supplier.Id);

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region GetSupplierPaymentHistoryAsync Tests

    [Fact]
    public async Task GetSupplierPaymentHistoryAsync_WithPayments_ShouldReturnPayments()
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
        var result = await _supplierService.GetSupplierPaymentHistoryAsync(supplier.Id);

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region UpdateSupplierAsync Tests

    [Fact]
    public async Task UpdateSupplierAsync_WithValidRequest_ShouldUpdateSupplier()
    {
        // Arrange
        var supplier = TestData.CreateSupplier().Build();
        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        var updateRequest = new UpdateSupplierRequest
        {
            ContactEmail = "newemail@company.com",
            PaymentThreshold = 2000m
        };
        var keyManagementService = new Mock<IKeyManagementService>();

        // Act
        var result = await _supplierService.UpdateSupplierAsync(supplier.Id, updateRequest, keyManagementService.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newemail@company.com", result.ContactEmail);
        Assert.Equal(2000m, result.PaymentThreshold);
    }

    #endregion
}
