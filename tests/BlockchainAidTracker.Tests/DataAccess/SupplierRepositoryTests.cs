using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Tests.Infrastructure;
using Xunit;

namespace BlockchainAidTracker.Tests.DataAccess;

public class SupplierRepositoryTests : DatabaseTestBase
{
    private readonly SupplierRepository _supplierRepository;

    public SupplierRepositoryTests()
    {
        _supplierRepository = new SupplierRepository(Context);
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidSupplier_ShouldAddToDatabase()
    {
        // Arrange
        var supplier = TestData.CreateSupplier()
            .WithCompanyName("Unique Company ABC")
            .Build();

        // Act
        var result = await _supplierRepository.AddAsync(supplier);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(supplier.Id, result.Id);
        Assert.Equal("Unique Company ABC", result.CompanyName);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingSupplier_ShouldReturnSupplier()
    {
        // Arrange
        var supplier = TestData.CreateSupplier().Build();
        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierRepository.GetByIdAsync(supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(supplier.Id, result.Id);
        Assert.Equal(supplier.CompanyName, result.CompanyName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingSupplier_ShouldReturnNull()
    {
        // Act
        var result = await _supplierRepository.GetByIdAsync("non-existing-id");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByCompanyNameAsync Tests

    [Fact]
    public async Task GetByCompanyNameAsync_ExistingCompanyName_ShouldReturnSupplier()
    {
        // Arrange
        var supplier = TestData.CreateSupplier()
            .WithCompanyName("UniqueCompanyName123")
            .Build();
        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierRepository.GetByCompanyNameAsync("UniqueCompanyName123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("UniqueCompanyName123", result.CompanyName);
    }

    [Fact]
    public async Task GetByCompanyNameAsync_NonExistingCompanyName_ShouldReturnNull()
    {
        // Act
        var result = await _supplierRepository.GetByCompanyNameAsync("NonExistingCompany");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByVerificationStatusAsync Tests

    [Fact]
    public async Task GetByVerificationStatusAsync_PendingSuppliers_ShouldReturnOnlyPending()
    {
        // Arrange
        var pendingSupplier = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Pending)
            .Build();
        var verifiedSupplier = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Verified)
            .Build();

        await Context.Suppliers.AddRangeAsync(pendingSupplier, verifiedSupplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierRepository.GetByVerificationStatusAsync(SupplierVerificationStatus.Pending);

        // Assert
        Assert.Single(result);
        Assert.Equal(pendingSupplier.Id, result[0].Id);
    }

    [Fact]
    public async Task GetByVerificationStatusAsync_VerifiedSuppliers_ShouldReturnOnlyVerified()
    {
        // Arrange
        var supplier1 = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Verified)
            .Build();
        var supplier2 = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Verified)
            .Build();

        await Context.Suppliers.AddRangeAsync(supplier1, supplier2);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierRepository.GetByVerificationStatusAsync(SupplierVerificationStatus.Verified);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(SupplierVerificationStatus.Verified, s.VerificationStatus));
    }

    #endregion

    #region GetActiveAsync Tests

    [Fact]
    public async Task GetActiveAsync_MixedSuppliers_ShouldReturnOnlyActive()
    {
        // Arrange
        var activeSupplier = TestData.CreateSupplier().Build(); // IsActive = true by default
        var inactiveSupplier = TestData.CreateSupplier().AsInactive().Build();

        await Context.Suppliers.AddRangeAsync(activeSupplier, inactiveSupplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierRepository.GetActiveAsync();

        // Assert
        Assert.Single(result);
        Assert.True(result[0].IsActive);
    }

    #endregion

    #region GetVerifiedAsync Tests

    [Fact]
    public async Task GetVerifiedAsync_MixedSuppliers_ShouldReturnOnlyVerifiedAndActive()
    {
        // Arrange
        var verified = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Verified)
            .Build();
        var verifiedButInactive = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Verified)
            .AsInactive()
            .Build();
        var pending = TestData.CreateSupplier()
            .WithVerificationStatus(SupplierVerificationStatus.Pending)
            .Build();

        await Context.Suppliers.AddRangeAsync(verified, verifiedButInactive, pending);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierRepository.GetVerifiedAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(verified.Id, result[0].Id);
        Assert.Equal(SupplierVerificationStatus.Verified, result[0].VerificationStatus);
        Assert.True(result[0].IsActive);
    }

    #endregion

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_ExistingUserId_ShouldReturnSupplier()
    {
        // Arrange
        var userId = "user-123";
        var supplier = TestData.CreateSupplier()
            .WithUserId(userId)
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierRepository.GetByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task GetByUserIdAsync_NonExistingUserId_ShouldReturnNull()
    {
        // Act
        var result = await _supplierRepository.GetByUserIdAsync("non-existing-user");

        // Assert
        Assert.Null(result);
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
        var supplierShipment1 = TestData.CreateSupplierShipment()
            .WithSupplierId(supplier.Id)
            .WithShipmentId(shipment1.Id)
            .Build();
        var supplierShipment2 = TestData.CreateSupplierShipment()
            .WithSupplierId(supplier.Id)
            .WithShipmentId(shipment2.Id)
            .Build();

        await Context.Suppliers.AddAsync(supplier);
        await Context.Shipments.AddRangeAsync(shipment1, shipment2);
        await Context.SupplierShipments.AddRangeAsync(supplierShipment1, supplierShipment2);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierRepository.GetSupplierShipmentsAsync(supplier.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, ss => Assert.Equal(supplier.Id, ss.SupplierId));
    }

    [Fact]
    public async Task GetSupplierShipmentsAsync_NoShipments_ShouldReturnEmptyList()
    {
        // Arrange
        var supplier = TestData.CreateSupplier().Build();
        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _supplierRepository.GetSupplierShipmentsAsync(supplier.Id);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region CompanyNameExistsAsync Tests

    [Fact]
    public async Task CompanyNameExistsAsync_ExistingCompanyName_ShouldReturnTrue()
    {
        // Arrange
        var supplier = TestData.CreateSupplier()
            .WithCompanyName("ExistingCompany123")
            .Build();
        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();

        // Act
        var result = await _supplierRepository.CompanyNameExistsAsync("ExistingCompany123");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CompanyNameExistsAsync_NonExistingCompanyName_ShouldReturnFalse()
    {
        // Act
        var result = await _supplierRepository.CompanyNameExistsAsync("NonExistingCompany");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region TaxIdExistsAsync Tests

    [Fact]
    public async Task TaxIdExistsAsync_ExistingTaxId_ShouldReturnTrue()
    {
        // Arrange
        var supplier = TestData.CreateSupplier().Build();
        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();

        // Act
        var result = await _supplierRepository.TaxIdExistsAsync(supplier.TaxId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TaxIdExistsAsync_NonExistingTaxId_ShouldReturnFalse()
    {
        // Act
        var result = await _supplierRepository.TaxIdExistsAsync("NONEXISTENT-TAX-ID");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingSupplier_ShouldUpdateSupplier()
    {
        // Arrange
        var supplier = TestData.CreateSupplier().Build();
        await Context.Suppliers.AddAsync(supplier);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        var dbSupplier = await _supplierRepository.GetByIdAsync(supplier.Id);
        dbSupplier!.VerificationStatus = SupplierVerificationStatus.Verified;

        // Act
        _supplierRepository.Update(dbSupplier);
        await Context.SaveChangesAsync();

        // Assert - Verify with new context
        using var newContext = CreateNewContext();
        var updated = await newContext.Suppliers.FindAsync(supplier.Id);
        Assert.NotNull(updated);
        Assert.Equal(SupplierVerificationStatus.Verified, updated.VerificationStatus);
    }

    #endregion
}
