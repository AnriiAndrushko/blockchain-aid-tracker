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

        // Create a block before checking transaction IDs
        await _factory.TriggerBlockCreationAsync();

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

        // Create a block to include all the status updates
        await _factory.TriggerBlockCreationAsync();

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

        // Create a block to include the transaction
        await _factory.TriggerBlockCreationAsync();

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

        // Create a block to include all status updates
        await _factory.TriggerBlockCreationAsync();

        // Step 5: Confirm delivery as recipient
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);
        var confirmResponse = await _client.PostAsync($"/api/shipments/{shipment.Id}/confirm-delivery", null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmedShipment = await confirmResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        confirmedShipment!.Status.Should().Be(ShipmentStatus.Confirmed);
        confirmedShipment.ActualDeliveryDate.Should().NotBeNull();

        // Create another block for the confirm delivery transaction
        await _factory.TriggerBlockCreationAsync();

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
}
