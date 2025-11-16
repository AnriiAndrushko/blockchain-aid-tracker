using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.Services.DTOs.Authentication;
using BlockchainAidTracker.Services.DTOs.Shipment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BlockchainAidTracker.Tests.Integration;

/// <summary>
/// Integration tests for ShipmentController
/// </summary>
public class ShipmentControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ShipmentControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a user directly in the database with the specified role and returns auth token
    /// </summary>
    private async Task<string> CreateUserAndGetTokenAsync(string username, UserRole role)
    {
        var (token, _) = await CreateUserAndGetTokenWithIdAsync(username, role);
        return token;
    }

    /// <summary>
    /// Creates a user directly in the database with the specified role and returns auth token + userId
    /// </summary>
    private async Task<(string Token, string UserId)> CreateUserAndGetTokenWithIdAsync(string username, UserRole role)
    {
        const string password = "SecurePassword123!";

        // Use the registration endpoint to create user (creates with Recipient role by default)
        var registerRequest = new RegisterRequest
        {
            FirstName = username,
            LastName = "User",
            Username = username,
            Email = $"{username}@example.com",
            Password = password,
            Organization = "Test Organization",
            Role = "Recipient"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        var userId = authResponse!.UserId;

        // Update the role in the database if it's not the default Recipient role
        if (role != UserRole.Recipient)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.Role = role;
                await dbContext.SaveChangesAsync();
            }
        }

        // Login to get a fresh token with the updated role
        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = username,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);
        var loginAuthResponse = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        return (loginAuthResponse!.AccessToken, userId);
    }

    private CreateShipmentRequest CreateValidShipmentRequest(string recipientId)
    {
        return new CreateShipmentRequest
        {
            Origin = "Warehouse A",
            Destination = "Distribution Center B",
            RecipientId = recipientId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new List<ShipmentItemDto>
            {
                new() { Description = "Food Supplies", Quantity = 100, Unit = "boxes" },
                new() { Description = "Medical Kits", Quantity = 50, Unit = "kits" }
            }
        };
    }

    #endregion

    #region POST /api/shipments Tests

    [Fact]
    public async Task CreateShipment_WithValidRequest_ReturnsCreatedWithShipmentDto()
    {
        // Arrange
        var (token, userId) = await CreateUserAndGetTokenWithIdAsync("coordinator1", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient1", UserRole.Recipient);
        var request = CreateValidShipmentRequest(recipientId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/shipments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ShipmentDto>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeNullOrEmpty();
        result.Origin.Should().Be(request.Origin);
        result.Destination.Should().Be(request.Destination);
        result.RecipientId.Should().Be(recipientId);
        result.Status.Should().Be(ShipmentStatus.Created);
        result.QrCode.Should().NotBeNullOrEmpty();
        result.Items.Should().HaveCount(2);
        result.TransactionIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateShipment_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient2", UserRole.Recipient);
        var request = CreateValidShipmentRequest(recipientId);

        // Act (no authorization header)
        var response = await _client.PostAsJsonAsync("/api/shipments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateShipment_WithInvalidRecipient_ReturnsBadRequest()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("coordinator2", UserRole.Coordinator);
        var request = CreateValidShipmentRequest("invalid-recipient-id");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/shipments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/shipments Tests

    [Fact]
    public async Task GetShipments_WithAuthentication_ReturnsOkWithShipmentList()
    {
        // Arrange
        var (token, userId) = await CreateUserAndGetTokenWithIdAsync("coordinator3", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient3", UserRole.Recipient);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a test shipment
        var createRequest = CreateValidShipmentRequest(recipientId);
        await _client.PostAsJsonAsync("/api/shipments", createRequest);

        // Act
        var response = await _client.GetAsync("/api/shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ShipmentDto>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetShipments_WithStatusFilter_ReturnsFilteredList()
    {
        // Arrange
        var (token, userId) = await CreateUserAndGetTokenWithIdAsync("coordinator4", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient4", UserRole.Recipient);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a test shipment
        var createRequest = CreateValidShipmentRequest(recipientId);
        await _client.PostAsJsonAsync("/api/shipments", createRequest);

        // Act
        var response = await _client.GetAsync($"/api/shipments?status={ShipmentStatus.Created}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ShipmentDto>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result!.All(s => s.Status == ShipmentStatus.Created).Should().BeTrue();
    }

    [Fact]
    public async Task GetShipments_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/shipments/{id} Tests

    [Fact]
    public async Task GetShipmentById_WithValidId_ReturnsOkWithShipment()
    {
        // Arrange
        var (token, userId) = await CreateUserAndGetTokenWithIdAsync("coordinator5", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient5", UserRole.Recipient);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a shipment
        var createRequest = CreateValidShipmentRequest(recipientId);
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>();

        // Act
        var response = await _client.GetAsync($"/api/shipments/{createdShipment!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ShipmentDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdShipment.Id);
        result.Origin.Should().Be(createRequest.Origin);
        result.Destination.Should().Be(createRequest.Destination);
    }

    [Fact]
    public async Task GetShipmentById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("coordinator6", UserRole.Coordinator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/shipments/invalid-id-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetShipmentById_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/shipments/some-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/shipments/{id}/status Tests

    [Fact]
    public async Task UpdateShipmentStatus_WithValidRequest_ReturnsOkWithUpdatedShipment()
    {
        // Arrange
        var (token, userId) = await CreateUserAndGetTokenWithIdAsync("coordinator7", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient7", UserRole.Recipient);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a shipment
        var createRequest = CreateValidShipmentRequest(recipientId);
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>();

        // First update to Validated (required transition from Created)
        var validateRequest = new UpdateShipmentStatusRequest
        {
            ShipmentId = createdShipment!.Id,
            NewStatus = ShipmentStatus.Validated
        };
        await _client.PutAsJsonAsync($"/api/shipments/{createdShipment.Id}/status", validateRequest);

        // Then update to InTransit
        var updateRequest = new UpdateShipmentStatusRequest
        {
            ShipmentId = createdShipment.Id,
            NewStatus = ShipmentStatus.InTransit,
            Notes = "Shipment departed warehouse"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/shipments/{createdShipment.Id}/status", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ShipmentDto>();
        result.Should().NotBeNull();
        result!.Status.Should().Be(ShipmentStatus.InTransit);
        result.TransactionIds.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task UpdateShipmentStatus_WithInvalidShipmentId_ReturnsNotFound()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("coordinator8", UserRole.Coordinator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateShipmentStatusRequest
        {
            ShipmentId = "invalid-id",
            NewStatus = ShipmentStatus.InTransit
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/shipments/invalid-id/status", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateShipmentStatus_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var updateRequest = new UpdateShipmentStatusRequest
        {
            ShipmentId = "some-id",
            NewStatus = ShipmentStatus.InTransit
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/shipments/some-id/status", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/shipments/{id}/confirm-delivery Tests

    [Fact]
    public async Task ConfirmDelivery_WithValidRecipient_ReturnsOkWithConfirmedShipment()
    {
        // Arrange
        var (coordinatorToken, coordinatorId) = await CreateUserAndGetTokenWithIdAsync("coordinator9", UserRole.Coordinator);
        var (recipientToken, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient9", UserRole.Recipient);

        // Create shipment as coordinator
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);
        var createRequest = CreateValidShipmentRequest(recipientId);
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>();

        // Follow the complete workflow: Created -> Validated -> InTransit -> Delivered
        // Step 1: Validate
        var validateRequest = new UpdateShipmentStatusRequest
        {
            ShipmentId = createdShipment!.Id,
            NewStatus = ShipmentStatus.Validated
        };
        await _client.PutAsJsonAsync($"/api/shipments/{createdShipment.Id}/status", validateRequest);

        // Step 2: InTransit
        var transitRequest = new UpdateShipmentStatusRequest
        {
            ShipmentId = createdShipment.Id,
            NewStatus = ShipmentStatus.InTransit
        };
        await _client.PutAsJsonAsync($"/api/shipments/{createdShipment.Id}/status", transitRequest);

        // Step 3: Delivered
        var deliveredRequest = new UpdateShipmentStatusRequest
        {
            ShipmentId = createdShipment.Id,
            NewStatus = ShipmentStatus.Delivered
        };
        await _client.PutAsJsonAsync($"/api/shipments/{createdShipment.Id}/status", deliveredRequest);

        // Switch to recipient token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);

        // Act
        var response = await _client.PostAsync($"/api/shipments/{createdShipment.Id}/confirm-delivery", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ShipmentDto>();
        result.Should().NotBeNull();
        result!.Status.Should().Be(ShipmentStatus.Confirmed);
        result.ActualDeliveryDate.Should().NotBeNull();
        result.TransactionIds.Should().HaveCountGreaterThan(2);
    }

    [Fact]
    public async Task ConfirmDelivery_WithInvalidShipmentId_ReturnsNotFound()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("recipient10", UserRole.Recipient);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync("/api/shipments/invalid-id/confirm-delivery", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConfirmDelivery_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/shipments/some-id/confirm-delivery", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/shipments/{id}/history Tests

    [Fact]
    public async Task GetShipmentHistory_WithValidId_ReturnsOkWithTransactionList()
    {
        // Arrange
        var (token, userId) = await CreateUserAndGetTokenWithIdAsync("coordinator10", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient11", UserRole.Recipient);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a shipment
        var createRequest = CreateValidShipmentRequest(recipientId);
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>();

        // Act
        var response = await _client.GetAsync($"/api/shipments/{createdShipment!.Id}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<string>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetShipmentHistory_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("coordinator11", UserRole.Coordinator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/shipments/invalid-id/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetShipmentHistory_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/shipments/some-id/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/shipments/{id}/qrcode Tests

    [Fact]
    public async Task GetShipmentQrCode_WithValidId_ReturnsOkWithPngImage()
    {
        // Arrange
        var (token, userId) = await CreateUserAndGetTokenWithIdAsync("coordinator12", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient12", UserRole.Recipient);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a shipment
        var createRequest = CreateValidShipmentRequest(recipientId);
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>();

        // Act
        var response = await _client.GetAsync($"/api/shipments/{createdShipment!.Id}/qrcode");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        var imageBytes = await response.Content.ReadAsByteArrayAsync();
        imageBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetShipmentQrCode_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("coordinator13", UserRole.Coordinator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/shipments/invalid-id/qrcode");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetShipmentQrCode_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/shipments/some-id/qrcode");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region End-to-End Workflow Tests

    [Fact]
    public async Task CompleteShipmentLifecycle_FromCreationToConfirmation_WorksCorrectly()
    {
        // Arrange - Create coordinator and recipient
        var (coordinatorToken, coordinatorId) = await CreateUserAndGetTokenWithIdAsync("coordinator-workflow", UserRole.Coordinator);
        var (recipientToken, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient-workflow", UserRole.Recipient);

        // Act & Assert - Step 1: Create shipment
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);
        var createRequest = CreateValidShipmentRequest(recipientId);
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var shipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        shipment!.Status.Should().Be(ShipmentStatus.Created);

        // Step 2: Update to Validated
        var validateUpdate = new UpdateShipmentStatusRequest
        {
            ShipmentId = shipment.Id,
            NewStatus = ShipmentStatus.Validated
        };
        var validateResponse = await _client.PutAsJsonAsync($"/api/shipments/{shipment.Id}/status", validateUpdate);
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var validatedShipment = await validateResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        validatedShipment!.Status.Should().Be(ShipmentStatus.Validated);

        // Step 3: Update to InTransit
        var transitUpdate = new UpdateShipmentStatusRequest
        {
            ShipmentId = shipment.Id,
            NewStatus = ShipmentStatus.InTransit
        };
        var transitResponse = await _client.PutAsJsonAsync($"/api/shipments/{shipment.Id}/status", transitUpdate);
        transitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var inTransitShipment = await transitResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        inTransitShipment!.Status.Should().Be(ShipmentStatus.InTransit);

        // Step 4: Update to Delivered
        var deliveredUpdate = new UpdateShipmentStatusRequest
        {
            ShipmentId = shipment.Id,
            NewStatus = ShipmentStatus.Delivered
        };
        var deliveredResponse = await _client.PutAsJsonAsync($"/api/shipments/{shipment.Id}/status", deliveredUpdate);
        deliveredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deliveredShipment = await deliveredResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        deliveredShipment!.Status.Should().Be(ShipmentStatus.Delivered);

        // Step 5: Confirm delivery as recipient
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);
        var confirmResponse = await _client.PostAsync($"/api/shipments/{shipment.Id}/confirm-delivery", null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmedShipment = await confirmResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        confirmedShipment!.Status.Should().Be(ShipmentStatus.Confirmed);
        confirmedShipment.ActualDeliveryDate.Should().NotBeNull();

        // Step 6: Verify blockchain history
        var historyResponse = await _client.GetAsync($"/api/shipments/{shipment.Id}/history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await historyResponse.Content.ReadFromJsonAsync<List<string>>();
        history.Should().NotBeEmpty();
        history.Should().HaveCountGreaterThanOrEqualTo(4); // Created + Validated + InTransit + Delivered + Confirmed

        // Step 7: Get QR code
        var qrResponse = await _client.GetAsync($"/api/shipments/{shipment.Id}/qrcode");
        qrResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        qrResponse.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
    }

    #endregion

    #region Donor and LogisticsPartner Endpoint Tests

    [Fact]
    public async Task GetMyDonorShipments_ReturnsDonorShipments()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var (coordinatorToken, coordinatorId) = await CreateUserAndGetTokenWithIdAsync($"coordinator_{uniqueId}", UserRole.Coordinator);
        var (donorToken, donorId) = await CreateUserAndGetTokenWithIdAsync($"donor_{uniqueId}", UserRole.Donor);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync($"recipient_{uniqueId}", UserRole.Recipient);

        // Create shipment with donor
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);
        var createRequest = new
        {
            Origin = "Origin1",
            Destination = "Destination1",
            RecipientId = recipientId,
            DonorId = donorId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new[]
            {
                new { Description = "Item1", Quantity = 10, Unit = "kg" }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Get donor's shipments
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", donorToken);
        var response = await _client.GetAsync("/api/shipments/donor/my-shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shipments = await response.Content.ReadFromJsonAsync<List<ShipmentDto>>();
        shipments.Should().NotBeNull();
        shipments.Should().HaveCount(1);
        shipments![0].DonorId.Should().Be(donorId);
    }

    [Fact]
    public async Task GetMyDonorShipments_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange - No authorization header

        // Act
        var response = await _client.GetAsync("/api/shipments/donor/my-shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyLogisticsPartnerShipments_ReturnsLogisticsPartnerShipments()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var (coordinatorToken, coordinatorId) = await CreateUserAndGetTokenWithIdAsync($"coordinator_{uniqueId}", UserRole.Coordinator);
        var (lpToken, lpId) = await CreateUserAndGetTokenWithIdAsync($"lp_{uniqueId}", UserRole.LogisticsPartner);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync($"recipient_{uniqueId}", UserRole.Recipient);

        // Create shipment with logistics partner
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);
        var createRequest = new
        {
            Origin = "Origin2",
            Destination = "Destination2",
            RecipientId = recipientId,
            LogisticsPartnerId = lpId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new[]
            {
                new { Description = "Item2", Quantity = 20, Unit = "boxes" }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Get logistics partner's shipments
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);
        var response = await _client.GetAsync("/api/shipments/logistics-partner/my-shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shipments = await response.Content.ReadFromJsonAsync<List<ShipmentDto>>();
        shipments.Should().NotBeNull();
        shipments.Should().HaveCount(1);
        shipments![0].LogisticsPartnerId.Should().Be(lpId);
    }

    [Fact]
    public async Task GetMyLogisticsPartnerShipments_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange - No authorization header

        // Act
        var response = await _client.GetAsync("/api/shipments/logistics-partner/my-shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateShipment_WithBothDonorAndLogisticsPartner_AssignsBoth()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var (coordinatorToken, coordinatorId) = await CreateUserAndGetTokenWithIdAsync($"coordinator_{uniqueId}", UserRole.Coordinator);
        var (_, donorId) = await CreateUserAndGetTokenWithIdAsync($"donor_{uniqueId}", UserRole.Donor);
        var (_, lpId) = await CreateUserAndGetTokenWithIdAsync($"lp_{uniqueId}", UserRole.LogisticsPartner);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync($"recipient_{uniqueId}", UserRole.Recipient);

        // Act - Create shipment with both donor and logistics partner
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);
        var createRequest = new
        {
            Origin = "Origin3",
            Destination = "Destination3",
            RecipientId = recipientId,
            DonorId = donorId,
            LogisticsPartnerId = lpId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new[]
            {
                new { Description = "Item3", Quantity = 30, Unit = "units" }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/shipments", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var shipment = await response.Content.ReadFromJsonAsync<ShipmentDto>();
        shipment.Should().NotBeNull();
        shipment!.DonorId.Should().Be(donorId);
        shipment.LogisticsPartnerId.Should().Be(lpId);
    }

    [Fact]
    public async Task CreateShipment_WithInvalidDonorRole_ReturnsBadRequest()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var (coordinatorToken, coordinatorId) = await CreateUserAndGetTokenWithIdAsync($"coordinator_{uniqueId}", UserRole.Coordinator);
        var (_, invalidDonorId) = await CreateUserAndGetTokenWithIdAsync($"invaliddonor_{uniqueId}", UserRole.Recipient); // Wrong role
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync($"recipient_{uniqueId}", UserRole.Recipient);

        // Act - Try to create shipment with invalid donor
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);
        var createRequest = new
        {
            Origin = "Origin4",
            Destination = "Destination4",
            RecipientId = recipientId,
            DonorId = invalidDonorId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new[]
            {
                new { Description = "Item4", Quantity = 40, Unit = "kg" }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/shipments", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateShipment_WithInvalidLogisticsPartnerRole_ReturnsBadRequest()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var (coordinatorToken, coordinatorId) = await CreateUserAndGetTokenWithIdAsync($"coordinator_{uniqueId}", UserRole.Coordinator);
        var (_, invalidLpId) = await CreateUserAndGetTokenWithIdAsync($"invalidlp_{uniqueId}", UserRole.Recipient); // Wrong role
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync($"recipient_{uniqueId}", UserRole.Recipient);

        // Act - Try to create shipment with invalid logistics partner
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);
        var createRequest = new
        {
            Origin = "Origin5",
            Destination = "Destination5",
            RecipientId = recipientId,
            LogisticsPartnerId = invalidLpId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new[]
            {
                new { Description = "Item5", Quantity = 50, Unit = "boxes" }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/shipments", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMyDonorShipments_EmptyListForDonorWithNoShipments()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var donorToken = await CreateUserAndGetTokenAsync($"donor_{uniqueId}", UserRole.Donor);

        // Act
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", donorToken);
        var response = await _client.GetAsync("/api/shipments/donor/my-shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shipments = await response.Content.ReadFromJsonAsync<List<ShipmentDto>>();
        shipments.Should().NotBeNull();
        shipments.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyLogisticsPartnerShipments_EmptyListForPartnerWithNoShipments()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var lpToken = await CreateUserAndGetTokenAsync($"lp_{uniqueId}", UserRole.LogisticsPartner);

        // Act
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);
        var response = await _client.GetAsync("/api/shipments/logistics-partner/my-shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shipments = await response.Content.ReadFromJsonAsync<List<ShipmentDto>>();
        shipments.Should().NotBeNull();
        shipments.Should().BeEmpty();
    }

    #endregion

    #region Complete Pipeline Test

    [Fact]
    public async Task CompleteShipmentPipeline_AllRoles_FullLifecycle()
    {
        // ============================================================
        // ARRANGE - Create all user types
        // ============================================================
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var testOutput = new System.Text.StringBuilder();

        testOutput.AppendLine("=== COMPLETE SHIPMENT PIPELINE TEST ===");
        testOutput.AppendLine($"Test ID: {uniqueId}");
        testOutput.AppendLine();

        // Step 1: Create all user types
        testOutput.AppendLine("Step 1: Creating all user types...");

        var (coordinatorToken, coordinatorId) = await CreateUserAndGetTokenWithIdAsync($"coordinator_{uniqueId}", UserRole.Coordinator);
        testOutput.AppendLine($"✓ Coordinator created: {coordinatorId[..8]}...");

        var (recipientToken, recipientId) = await CreateUserAndGetTokenWithIdAsync($"recipient_{uniqueId}", UserRole.Recipient);
        testOutput.AppendLine($"✓ Recipient created: {recipientId[..8]}...");

        var (donorToken, donorId) = await CreateUserAndGetTokenWithIdAsync($"donor_{uniqueId}", UserRole.Donor);
        testOutput.AppendLine($"✓ Donor created: {donorId[..8]}...");

        var (lpToken, lpId) = await CreateUserAndGetTokenWithIdAsync($"lp_{uniqueId}", UserRole.LogisticsPartner);
        testOutput.AppendLine($"✓ Logistics Partner created: {lpId[..8]}...");

        var (adminToken, adminId) = await CreateUserAndGetTokenWithIdAsync($"admin_{uniqueId}", UserRole.Administrator);
        testOutput.AppendLine($"✓ Administrator created: {adminId[..8]}...");

        var (validatorToken, validatorId) = await CreateUserAndGetTokenWithIdAsync($"validator_{uniqueId}", UserRole.Validator);
        testOutput.AppendLine($"✓ Validator created: {validatorId[..8]}...");

        testOutput.AppendLine();

        // ============================================================
        // ACT & ASSERT - Step 2: Coordinator creates shipment with Donor and LP
        // ============================================================
        testOutput.AppendLine("Step 2: Coordinator creating shipment...");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);

        var createRequest = new
        {
            Origin = "Warehouse Alpha, New York",
            Destination = "Relief Camp Beta, Somalia",
            RecipientId = recipientId,
            DonorId = donorId,
            LogisticsPartnerId = lpId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(14),
            Items = new[]
            {
                new { Description = "Medical Supplies", Quantity = 50, Unit = "boxes" },
                new { Description = "Water Purification Tablets", Quantity = 1000, Unit = "units" },
                new { Description = "Emergency Food Rations", Quantity = 200, Unit = "kg" }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var shipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        shipment.Should().NotBeNull();
        testOutput.AppendLine($"✓ Shipment created: {shipment!.Id[..8]}...");
        testOutput.AppendLine($"  - Origin: {shipment.Origin}");
        testOutput.AppendLine($"  - Destination: {shipment.Destination}");
        testOutput.AppendLine($"  - Items: {shipment.Items.Count}");
        testOutput.AppendLine($"  - Status: {shipment.Status}");
        testOutput.AppendLine($"  - Donor: {shipment.DonorId?[..8]}...");
        testOutput.AppendLine($"  - Logistics Partner: {shipment.LogisticsPartnerId?[..8]}...");

        // Verify initial state
        shipment.Status.Should().Be(ShipmentStatus.Created);
        shipment.DonorId.Should().Be(donorId);
        shipment.LogisticsPartnerId.Should().Be(lpId);
        shipment.RecipientId.Should().Be(recipientId);
        shipment.Items.Should().HaveCount(3);
        testOutput.AppendLine();

        // ============================================================
        // Step 3: Donor verifies they can see the shipment
        // ============================================================
        testOutput.AppendLine("Step 3: Donor checking funded shipments...");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", donorToken);

        var donorShipmentsResponse = await _client.GetAsync("/api/shipments/donor/my-shipments");
        donorShipmentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var donorShipments = await donorShipmentsResponse.Content.ReadFromJsonAsync<List<ShipmentDto>>();
        donorShipments.Should().NotBeNull();
        donorShipments.Should().HaveCount(1);
        donorShipments![0].Id.Should().Be(shipment.Id);
        testOutput.AppendLine($"✓ Donor can see their funded shipment");
        testOutput.AppendLine();

        // ============================================================
        // Step 4: Logistics Partner sees the assigned shipment
        // ============================================================
        testOutput.AppendLine("Step 4: Logistics Partner checking assigned deliveries...");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        var lpShipmentsResponse = await _client.GetAsync("/api/shipments/logistics-partner/my-shipments");
        lpShipmentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var lpShipments = await lpShipmentsResponse.Content.ReadFromJsonAsync<List<ShipmentDto>>();
        lpShipments.Should().NotBeNull();
        lpShipments.Should().HaveCount(1);
        lpShipments![0].Id.Should().Be(shipment.Id);
        testOutput.AppendLine($"✓ Logistics Partner can see assigned delivery");
        testOutput.AppendLine();

        // ============================================================
        // Step 5: Coordinator validates the shipment
        // ============================================================
        testOutput.AppendLine("Step 5: Coordinator validating shipment...");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);

        var validateRequest = new { NewStatus = "Validated" };
        var validateResponse = await _client.PutAsJsonAsync($"/api/shipments/{shipment.Id}/status", validateRequest);
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var validatedShipment = await validateResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        validatedShipment!.Status.Should().Be(ShipmentStatus.Validated);
        testOutput.AppendLine($"✓ Shipment status: Created → Validated");
        testOutput.AppendLine();

        // ============================================================
        // Step 6: Logistics Partner marks shipment as In Transit
        // ============================================================
        testOutput.AppendLine("Step 6: Logistics Partner starting transit...");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        var inTransitRequest = new { NewStatus = "InTransit" };
        var inTransitResponse = await _client.PutAsJsonAsync($"/api/shipments/{shipment.Id}/status", inTransitRequest);
        inTransitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var inTransitShipment = await inTransitResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        inTransitShipment!.Status.Should().Be(ShipmentStatus.InTransit);
        testOutput.AppendLine($"✓ Shipment status: Validated → InTransit");
        testOutput.AppendLine();

        // ============================================================
        // Step 7: Wait for blockchain transaction processing
        // ============================================================
        testOutput.AppendLine("Step 7: Waiting for blockchain processing...");
        await Task.Delay(500); // Give time for transactions to be processed
        testOutput.AppendLine($"✓ Blockchain transactions processed");
        testOutput.AppendLine();

        // ============================================================
        // Step 8: Logistics Partner marks shipment as Delivered
        // ============================================================
        testOutput.AppendLine("Step 8: Logistics Partner marking as delivered...");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        var deliveredRequest = new { NewStatus = "Delivered" };
        var deliveredResponse = await _client.PutAsJsonAsync($"/api/shipments/{shipment.Id}/status", deliveredRequest);
        deliveredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deliveredShipment = await deliveredResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        deliveredShipment!.Status.Should().Be(ShipmentStatus.Delivered);
        testOutput.AppendLine($"✓ Shipment status: InTransit → Delivered");
        testOutput.AppendLine();

        // ============================================================
        // Step 9: Recipient confirms delivery
        // ============================================================
        testOutput.AppendLine("Step 9: Recipient confirming delivery...");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);

        var confirmResponse = await _client.PostAsync($"/api/shipments/{shipment.Id}/confirm-delivery", null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmedShipment = await confirmResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        confirmedShipment!.Status.Should().Be(ShipmentStatus.Confirmed);
        confirmedShipment.ActualDeliveryDate.Should().NotBeNull();
        testOutput.AppendLine($"✓ Shipment status: Delivered → Confirmed");
        testOutput.AppendLine($"  - Actual delivery date: {confirmedShipment.ActualDeliveryDate:yyyy-MM-dd HH:mm:ss}");
        testOutput.AppendLine();

        // ============================================================
        // Step 10: Verify blockchain history
        // ============================================================
        testOutput.AppendLine("Step 10: Verifying blockchain history...");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);

        var historyResponse = await _client.GetAsync($"/api/shipments/{shipment.Id}/history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await historyResponse.Content.ReadFromJsonAsync<List<string>>();
        history.Should().NotBeNull();
        history.Should().NotBeEmpty();
        testOutput.AppendLine($"✓ Blockchain transactions: {history!.Count}");
        testOutput.AppendLine($"  - Expected: ShipmentCreated, StatusUpdated (x3), DeliveryConfirmed");

        // Should have: Created + 3 status updates + Delivery confirmation = 5 transactions minimum
        history.Should().HaveCountGreaterThanOrEqualTo(5);
        testOutput.AppendLine();

        // ============================================================
        // Step 11: Get QR code
        // ============================================================
        testOutput.AppendLine("Step 11: Retrieving shipment QR code...");
        var qrResponse = await _client.GetAsync($"/api/shipments/{shipment.Id}/qrcode");
        qrResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        qrResponse.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        var qrCodeBytes = await qrResponse.Content.ReadAsByteArrayAsync();
        qrCodeBytes.Should().NotBeEmpty();
        testOutput.AppendLine($"✓ QR code generated: {qrCodeBytes.Length} bytes");
        testOutput.AppendLine();

        // ============================================================
        // Step 12: Verify final state from all user perspectives
        // ============================================================
        testOutput.AppendLine("Step 12: Verifying final state from all perspectives...");

        // Coordinator view
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);
        var coordinatorViewResponse = await _client.GetAsync($"/api/shipments/{shipment.Id}");
        coordinatorViewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var coordinatorView = await coordinatorViewResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        coordinatorView!.Status.Should().Be(ShipmentStatus.Confirmed);
        testOutput.AppendLine($"✓ Coordinator view: Status = {coordinatorView.Status}");

        // Donor view
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", donorToken);
        var donorViewResponse = await _client.GetAsync($"/api/shipments/{shipment.Id}");
        donorViewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var donorView = await donorViewResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        donorView!.Status.Should().Be(ShipmentStatus.Confirmed);
        testOutput.AppendLine($"✓ Donor view: Status = {donorView.Status}");

        // Logistics Partner view
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);
        var lpViewResponse = await _client.GetAsync($"/api/shipments/{shipment.Id}");
        lpViewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var lpView = await lpViewResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        lpView!.Status.Should().Be(ShipmentStatus.Confirmed);
        testOutput.AppendLine($"✓ Logistics Partner view: Status = {lpView.Status}");

        // Recipient view
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);
        var recipientViewResponse = await _client.GetAsync($"/api/shipments/{shipment.Id}");
        recipientViewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var recipientView = await recipientViewResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        recipientView!.Status.Should().Be(ShipmentStatus.Confirmed);
        testOutput.AppendLine($"✓ Recipient view: Status = {recipientView.Status}");

        // Administrator view
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var adminViewResponse = await _client.GetAsync($"/api/shipments/{shipment.Id}");
        adminViewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminView = await adminViewResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        adminView!.Status.Should().Be(ShipmentStatus.Confirmed);
        testOutput.AppendLine($"✓ Administrator view: Status = {adminView.Status}");

        testOutput.AppendLine();

        // ============================================================
        // Step 13: Validate blockchain integrity
        // ============================================================
        testOutput.AppendLine("Step 13: Validating blockchain integrity...");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var validateBlockchainResponse = await _client.PostAsync("/api/blockchain/validate", null);
        validateBlockchainResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var validationResult = await validateBlockchainResponse.Content.ReadFromJsonAsync<ValidationResultDto>();
        validationResult.Should().NotBeNull();
        validationResult!.IsValid.Should().BeTrue();
        testOutput.AppendLine($"✓ Blockchain validation: {(validationResult.IsValid ? "PASSED" : "FAILED")}");
        if (!string.IsNullOrEmpty(validationResult.Message))
        {
            testOutput.AppendLine($"  - Message: {validationResult.Message}");
        }
        testOutput.AppendLine();

        // ============================================================
        // FINAL SUMMARY
        // ============================================================
        testOutput.AppendLine("=== TEST COMPLETE ===");
        testOutput.AppendLine("Summary:");
        testOutput.AppendLine($"✓ 6 user types created (Coordinator, Recipient, Donor, LP, Admin, Validator)");
        testOutput.AppendLine($"✓ Shipment created with 3 items");
        testOutput.AppendLine($"✓ Donor and Logistics Partner assigned");
        testOutput.AppendLine($"✓ Complete lifecycle: Created → Validated → InTransit → Delivered → Confirmed");
        testOutput.AppendLine($"✓ All role-based views verified");
        testOutput.AppendLine($"✓ Blockchain transactions recorded: {history.Count}");
        testOutput.AppendLine($"✓ Blockchain integrity validated");
        testOutput.AppendLine($"✓ QR code generated");
        testOutput.AppendLine();
        testOutput.AppendLine("Pipeline Status: SUCCESS ✅");

        // Output the test log
        Console.WriteLine(testOutput.ToString());
    }

    #endregion
}
