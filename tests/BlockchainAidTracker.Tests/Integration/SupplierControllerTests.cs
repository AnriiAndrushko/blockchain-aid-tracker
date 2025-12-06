using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.Authentication;
using BlockchainAidTracker.Services.DTOs.Supplier;
using FluentAssertions;

namespace BlockchainAidTracker.Tests.Integration;

/// <summary>
/// Integration tests for SupplierController
/// Tests all 7 endpoints with comprehensive scenarios
/// </summary>
public class SupplierControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public SupplierControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    /// <summary>
    /// Registers and authenticates a user with the specified role
    /// </summary>
    private async Task<(string UserId, string AccessToken)> RegisterAndAuthenticateUserAsync(string username, string email, string role)
    {
        var registerRequest = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Username = username,
            Email = email,
            Password = "SecurePassword123!",
            Organization = "Test Organization",
            Role = role
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        return (authResponse!.UserId, authResponse.AccessToken);
    }

    /// <summary>
    /// Creates a valid supplier registration request
    /// </summary>
    private CreateSupplierRequest CreateValidSupplierRequest(string companyName = "Test Company Ltd")
    {
        return new CreateSupplierRequest
        {
            CompanyName = companyName,
            RegistrationId = $"REG-{Guid.NewGuid().ToString().Substring(0, 8)}",
            ContactEmail = $"contact.{Guid.NewGuid().ToString().Substring(0, 8)}@testcompany.com",
            ContactPhone = "+1234567890",
            BusinessCategory = "Food",
            BankDetails = "IBAN: GB29NWBK60161331926819",
            PaymentThreshold = 1000m,
            TaxId = $"TAX-{Guid.NewGuid().ToString().Substring(0, 8)}"
        };
    }

    /// <summary>
    /// Registers a supplier and returns the supplier DTO
    /// </summary>
    private async Task<SupplierDto> RegisterSupplierAsync(string accessToken, CreateSupplierRequest request)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.PostAsJsonAsync("/api/suppliers", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SupplierDto>())!;
    }

    #endregion

    #region POST /api/suppliers - Register Supplier

    [Fact]
    public async Task RegisterSupplier_WithValidRequest_ReturnsCreatedWithSupplierDto()
    {
        // Arrange
        var (userId, accessToken) = await RegisterAndAuthenticateUserAsync("customer1", "customer1@example.com", "Customer");
        var request = CreateValidSupplierRequest("Valid Company Ltd");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/suppliers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var supplier = await response.Content.ReadFromJsonAsync<SupplierDto>();

        supplier.Should().NotBeNull();
        supplier!.Id.Should().NotBeNullOrEmpty();
        supplier.CompanyName.Should().Be("Valid Company Ltd");
        supplier.ContactEmail.Should().Be(request.ContactEmail);
        supplier.ContactPhone.Should().Be(request.ContactPhone);
        supplier.BusinessCategory.Should().Be("Food");
        supplier.PaymentThreshold.Should().Be(1000m);
        supplier.VerificationStatus.Should().Be("Pending");
        supplier.IsActive.Should().BeTrue();
        supplier.CreatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify Location header points to GetSupplierById
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/suppliers/{supplier.Id}");
    }

    [Fact]
    public async Task RegisterSupplier_WithDuplicateCompanyName_ReturnsBadRequest()
    {
        // Arrange
        var (userId, accessToken) = await RegisterAndAuthenticateUserAsync("customer2", "customer2@example.com", "Customer");
        var request1 = CreateValidSupplierRequest("Duplicate Company");
        var request2 = CreateValidSupplierRequest("Duplicate Company");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Register first supplier
        await _client.PostAsJsonAsync("/api/suppliers", request1);

        // Act - Try to register with same company name
        var response = await _client.PostAsJsonAsync("/api/suppliers", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterSupplier_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = CreateValidSupplierRequest();

        // Act - No authentication header
        var response = await _client.PostAsJsonAsync("/api/suppliers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterSupplier_WithNonCustomerRole_ReturnsForbidden()
    {
        // Arrange
        var (userId, accessToken) = await RegisterAndAuthenticateUserAsync("recipient1", "recipient1@example.com", "Recipient");
        var request = CreateValidSupplierRequest();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/suppliers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/suppliers/{id} - Get Supplier By ID

    [Fact]
    public async Task GetSupplierById_AsOwner_ReturnsOkWithSupplier()
    {
        // Arrange
        var (userId, accessToken) = await RegisterAndAuthenticateUserAsync("customer3", "customer3@example.com", "Customer");
        var request = CreateValidSupplierRequest("Owner Company");
        var supplier = await RegisterSupplierAsync(accessToken, request);

        // Act
        var response = await _client.GetAsync($"/api/suppliers/{supplier.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedSupplier = await response.Content.ReadFromJsonAsync<SupplierDto>();
        retrievedSupplier.Should().NotBeNull();
        retrievedSupplier!.Id.Should().Be(supplier.Id);
        retrievedSupplier.CompanyName.Should().Be("Owner Company");
    }

    [Fact]
    public async Task GetSupplierById_AsAdmin_ReturnsOkWithSupplier()
    {
        // Arrange
        var (customerUserId, customerToken) = await RegisterAndAuthenticateUserAsync("customer4", "customer4@example.com", "Customer");
        var (adminUserId, adminToken) = await RegisterAndAuthenticateUserAsync("admin1", "admin1@example.com", "Administrator");

        var request = CreateValidSupplierRequest("Admin View Company");
        var supplier = await RegisterSupplierAsync(customerToken, request);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync($"/api/suppliers/{supplier.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedSupplier = await response.Content.ReadFromJsonAsync<SupplierDto>();
        retrievedSupplier.Should().NotBeNull();
        retrievedSupplier!.CompanyName.Should().Be("Admin View Company");
    }

    [Fact]
    public async Task GetSupplierById_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        var (customer1Id, customer1Token) = await RegisterAndAuthenticateUserAsync("customer5", "customer5@example.com", "Customer");
        var (customer2Id, customer2Token) = await RegisterAndAuthenticateUserAsync("customer6", "customer6@example.com", "Customer");

        var request = CreateValidSupplierRequest("Forbidden Company");
        var supplier = await RegisterSupplierAsync(customer1Token, request);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer2Token);

        // Act
        var response = await _client.GetAsync($"/api/suppliers/{supplier.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSupplierById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var (userId, accessToken) = await RegisterAndAuthenticateUserAsync("admin2", "admin2@example.com", "Administrator");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.GetAsync($"/api/suppliers/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/suppliers - Get All Suppliers (Admin Only)

    [Fact]
    public async Task GetAllSuppliers_AsAdmin_ReturnsOkWithSupplierList()
    {
        // Arrange
        var (customerId, customerToken) = await RegisterAndAuthenticateUserAsync("customer7", "customer7@example.com", "Customer");
        var (adminId, adminToken) = await RegisterAndAuthenticateUserAsync("admin3", "admin3@example.com", "Administrator");

        // Register multiple suppliers with unique company names
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        await RegisterSupplierAsync(customerToken, CreateValidSupplierRequest($"Company A {uniqueSuffix}"));

        // Need a second customer to register a second supplier since one user can only have one supplier
        var (customer2Id, customer2Token) = await RegisterAndAuthenticateUserAsync("customer7b", "customer7b@example.com", "Customer");
        await RegisterSupplierAsync(customer2Token, CreateValidSupplierRequest($"Company B {uniqueSuffix}"));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/suppliers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var suppliers = await response.Content.ReadFromJsonAsync<List<SupplierDto>>();
        suppliers.Should().NotBeNull();
        suppliers!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAllSuppliers_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var (customerId, customerToken) = await RegisterAndAuthenticateUserAsync("customer8", "customer8@example.com", "Customer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        // Act
        var response = await _client.GetAsync("/api/suppliers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllSuppliers_WithActiveOnlyFilter_ReturnsOnlyActiveSuppliers()
    {
        // Arrange
        var (customerId, customerToken) = await RegisterAndAuthenticateUserAsync("customer9", "customer9@example.com", "Customer");
        var (adminId, adminToken) = await RegisterAndAuthenticateUserAsync("admin4", "admin4@example.com", "Administrator");

        await RegisterSupplierAsync(customerToken, CreateValidSupplierRequest("Active Company"));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/suppliers?activeOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var suppliers = await response.Content.ReadFromJsonAsync<List<SupplierDto>>();
        suppliers.Should().NotBeNull();
        suppliers!.All(s => s.IsActive).Should().BeTrue();
    }

    #endregion

    #region PUT /api/suppliers/{id} - Update Supplier

    [Fact]
    public async Task UpdateSupplier_AsOwner_ReturnsOkWithUpdatedSupplier()
    {
        // Arrange
        var (userId, accessToken) = await RegisterAndAuthenticateUserAsync("customer10", "customer10@example.com", "Customer");
        var createRequest = CreateValidSupplierRequest("Original Company");
        var supplier = await RegisterSupplierAsync(accessToken, createRequest);

        var updateRequest = new UpdateSupplierRequest
        {
            ContactEmail = "updated@testcompany.com",
            ContactPhone = "+9876543210",
            PaymentThreshold = 2000m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/suppliers/{supplier.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedSupplier = await response.Content.ReadFromJsonAsync<SupplierDto>();
        updatedSupplier.Should().NotBeNull();
        updatedSupplier!.ContactEmail.Should().Be("updated@testcompany.com");
        updatedSupplier.ContactPhone.Should().Be("+9876543210");
        updatedSupplier.PaymentThreshold.Should().Be(2000m);
        updatedSupplier.CompanyName.Should().Be("Original Company"); // Should not change
    }

    [Fact]
    public async Task UpdateSupplier_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        var (customer1Id, customer1Token) = await RegisterAndAuthenticateUserAsync("customer11", "customer11@example.com", "Customer");
        var (customer2Id, customer2Token) = await RegisterAndAuthenticateUserAsync("customer12", "customer12@example.com", "Customer");

        var createRequest = CreateValidSupplierRequest($"Original Company {Guid.NewGuid().ToString().Substring(0, 8)}");
        var supplier = await RegisterSupplierAsync(customer1Token, createRequest);

        var updateRequest = new UpdateSupplierRequest
        {
            ContactEmail = "hacker@evil.com",
            ContactPhone = "+1111111111"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer2Token);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/suppliers/{supplier.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSupplier_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var (userId, accessToken) = await RegisterAndAuthenticateUserAsync("admin5", "admin5@example.com", "Administrator");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var updateRequest = new UpdateSupplierRequest
        {
            ContactEmail = "test@test.com",
            ContactPhone = "+1234567890"
        };

        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/suppliers/{nonExistentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/suppliers/{id}/verify - Verify/Reject Supplier

    [Fact]
    public async Task VerifySupplier_AsAdmin_ReturnsOkAndUpdatesStatus()
    {
        // Arrange
        var (customerId, customerToken) = await RegisterAndAuthenticateUserAsync("customer13", "customer13@example.com", "Customer");
        var (adminId, adminToken) = await RegisterAndAuthenticateUserAsync("admin6", "admin6@example.com", "Administrator");

        var createRequest = CreateValidSupplierRequest("Verify Company");
        var supplier = await RegisterSupplierAsync(customerToken, createRequest);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsync($"/api/suppliers/{supplier.Id}/verify?status=Verified", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the status was updated
        var getResponse = await _client.GetAsync($"/api/suppliers/{supplier.Id}");
        var verifiedSupplier = await getResponse.Content.ReadFromJsonAsync<SupplierDto>();
        verifiedSupplier!.VerificationStatus.Should().Be("Verified");
    }

    [Fact]
    public async Task VerifySupplier_WithRejectedStatus_UpdatesStatusToRejected()
    {
        // Arrange
        var (customerId, customerToken) = await RegisterAndAuthenticateUserAsync("customer14", "customer14@example.com", "Customer");
        var (adminId, adminToken) = await RegisterAndAuthenticateUserAsync("admin7", "admin7@example.com", "Administrator");

        var createRequest = CreateValidSupplierRequest("Reject Company");
        var supplier = await RegisterSupplierAsync(customerToken, createRequest);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsync($"/api/suppliers/{supplier.Id}/verify?status=Rejected", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the status was updated
        var getResponse = await _client.GetAsync($"/api/suppliers/{supplier.Id}");
        var rejectedSupplier = await getResponse.Content.ReadFromJsonAsync<SupplierDto>();
        rejectedSupplier!.VerificationStatus.Should().Be("Rejected");
    }

    [Fact]
    public async Task VerifySupplier_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var (customerId, customerToken) = await RegisterAndAuthenticateUserAsync("customer15", "customer15@example.com", "Customer");
        var createRequest = CreateValidSupplierRequest("Unauthorized Verify");
        var supplier = await RegisterSupplierAsync(customerToken, createRequest);

        // Act - Try to verify as customer
        var response = await _client.PostAsync($"/api/suppliers/{supplier.Id}/verify?status=Verified", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VerifySupplier_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        var (customerId, customerToken) = await RegisterAndAuthenticateUserAsync("customer16", "customer16@example.com", "Customer");
        var (adminId, adminToken) = await RegisterAndAuthenticateUserAsync("admin8", "admin8@example.com", "Administrator");

        var createRequest = CreateValidSupplierRequest("Invalid Status Company");
        var supplier = await RegisterSupplierAsync(customerToken, createRequest);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsync($"/api/suppliers/{supplier.Id}/verify?status=Invalid", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/suppliers/{id}/activate - Activate Supplier

    [Fact]
    public async Task ActivateSupplier_AsAdmin_ReturnsOk()
    {
        // Arrange
        var (customerId, customerToken) = await RegisterAndAuthenticateUserAsync("customer17", "customer17@example.com", "Customer");
        var (adminId, adminToken) = await RegisterAndAuthenticateUserAsync("admin9", "admin9@example.com", "Administrator");

        var createRequest = CreateValidSupplierRequest("Activate Company");
        var supplier = await RegisterSupplierAsync(customerToken, createRequest);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsync($"/api/suppliers/{supplier.Id}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ActivateSupplier_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var (customerId, customerToken) = await RegisterAndAuthenticateUserAsync("customer18", "customer18@example.com", "Customer");
        var createRequest = CreateValidSupplierRequest("Unauthorized Activate");
        var supplier = await RegisterSupplierAsync(customerToken, createRequest);

        // Act - Try to activate as customer
        var response = await _client.PostAsync($"/api/suppliers/{supplier.Id}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/suppliers/{id}/deactivate - Deactivate Supplier

    [Fact]
    public async Task DeactivateSupplier_AsAdmin_ReturnsOk()
    {
        // Arrange
        var (customerId, customerToken) = await RegisterAndAuthenticateUserAsync("customer19", "customer19@example.com", "Customer");
        var (adminId, adminToken) = await RegisterAndAuthenticateUserAsync("admin10", "admin10@example.com", "Administrator");

        var createRequest = CreateValidSupplierRequest("Deactivate Company");
        var supplier = await RegisterSupplierAsync(customerToken, createRequest);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsync($"/api/suppliers/{supplier.Id}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the supplier is now inactive
        var getResponse = await _client.GetAsync($"/api/suppliers/{supplier.Id}");
        var deactivatedSupplier = await getResponse.Content.ReadFromJsonAsync<SupplierDto>();
        deactivatedSupplier!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateSupplier_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var (customerId, customerToken) = await RegisterAndAuthenticateUserAsync("customer20", "customer20@example.com", "Customer");
        var createRequest = CreateValidSupplierRequest("Unauthorized Deactivate");
        var supplier = await RegisterSupplierAsync(customerToken, createRequest);

        // Act - Try to deactivate as customer
        var response = await _client.PostAsync($"/api/suppliers/{supplier.Id}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/suppliers/{id}/payments - Get Supplier Payments

    [Fact]
    public async Task GetSupplierPayments_AsOwner_ReturnsOk()
    {
        // Arrange
        var (userId, accessToken) = await RegisterAndAuthenticateUserAsync("customer21", "customer21@example.com", "Customer");
        var createRequest = CreateValidSupplierRequest("Payment History Company");
        var supplier = await RegisterSupplierAsync(accessToken, createRequest);

        // Act
        var response = await _client.GetAsync($"/api/suppliers/{supplier.Id}/payments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await response.Content.ReadFromJsonAsync<List<object>>();
        payments.Should().NotBeNull();
        // Should be empty initially, but endpoint should work
    }

    [Fact]
    public async Task GetSupplierPayments_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        var (customer1Id, customer1Token) = await RegisterAndAuthenticateUserAsync("customer22", "customer22@example.com", "Customer");
        var (customer2Id, customer2Token) = await RegisterAndAuthenticateUserAsync("customer23", "customer23@example.com", "Customer");

        var createRequest = CreateValidSupplierRequest("Forbidden Payments Company");
        var supplier = await RegisterSupplierAsync(customer1Token, createRequest);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer2Token);

        // Act
        var response = await _client.GetAsync($"/api/suppliers/{supplier.Id}/payments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSupplierPayments_AsAdmin_ReturnsOk()
    {
        // Arrange
        var (customerId, customerToken) = await RegisterAndAuthenticateUserAsync("customer24", "customer24@example.com", "Customer");
        var (adminId, adminToken) = await RegisterAndAuthenticateUserAsync("admin11", "admin11@example.com", "Administrator");

        var createRequest = CreateValidSupplierRequest("Admin Payment View Company");
        var supplier = await RegisterSupplierAsync(customerToken, createRequest);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync($"/api/suppliers/{supplier.Id}/payments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
