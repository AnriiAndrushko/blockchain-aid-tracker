using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.Services.DTOs.Authentication;
using BlockchainAidTracker.Services.DTOs.LogisticsPartner;
using BlockchainAidTracker.Services.DTOs.Shipment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BlockchainAidTracker.Tests.Integration;

/// <summary>
/// Integration tests for LogisticsPartnerController API endpoints
/// Tests all 7 REST API endpoints with authentication, authorization, and error scenarios
/// </summary>
public class LogisticsPartnerControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public LogisticsPartnerControllerTests(CustomWebApplicationFactory factory)
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

        // Use the registration endpoint to create user
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

    /// <summary>
    /// Creates a shipment in the database for testing
    /// </summary>
    private async Task<string> CreateTestShipmentAsync(string recipientId, string coordinatorToken)
    {
        var request = new CreateShipmentRequest
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

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);
        var response = await _client.PostAsJsonAsync("/api/shipments", request);
        var result = await response.Content.ReadFromJsonAsync<ShipmentDto>();
        return result!.Id;
    }

    /// <summary>
    /// Updates shipment status to InTransit for testing logistics partner operations
    /// Must go through proper lifecycle: Created → Validated → InTransit
    /// </summary>
    private async Task UpdateShipmentToInTransitAsync(string shipmentId, string coordinatorToken)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);

        // First update to Validated
        var validateRequest = new UpdateShipmentStatusRequest
        {
            NewStatus = ShipmentStatus.Validated
        };
        await _client.PutAsJsonAsync($"/api/shipments/{shipmentId}/status", validateRequest);

        // Then update to InTransit
        var inTransitRequest = new UpdateShipmentStatusRequest
        {
            NewStatus = ShipmentStatus.InTransit
        };
        await _client.PutAsJsonAsync($"/api/shipments/{shipmentId}/status", inTransitRequest);
    }

    #endregion

    #region GET /api/logistics-partner/shipments Tests

    [Fact]
    public async Task GetAssignedShipments_WithLogisticsPartnerRole_ReturnsShipmentsList()
    {
        // Arrange
        var (lpToken, lpUserId) = await CreateUserAndGetTokenWithIdAsync("logistics1", UserRole.LogisticsPartner);
        var (coordToken, _) = await CreateUserAndGetTokenWithIdAsync("coordinator1", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient1", UserRole.Recipient);

        var shipmentId = await CreateTestShipmentAsync(recipientId, coordToken);
        await UpdateShipmentToInTransitAsync(shipmentId, coordToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.GetAsync("/api/logistics-partner/shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ShipmentDto>>();
        result.Should().NotBeNull();
        result!.Should().BeOfType<List<ShipmentDto>>();
    }

    [Fact]
    public async Task GetAssignedShipments_WithAdministratorRole_ReturnsShipmentsList()
    {
        // Arrange - Administrators should also be able to view shipments
        var (adminToken, _) = await CreateUserAndGetTokenWithIdAsync("admin1", UserRole.Administrator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/logistics-partner/shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ShipmentDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAssignedShipments_WithInvalidRole_ReturnsForbidden()
    {
        // Arrange - Donor role should not have access
        var donorToken = await CreateUserAndGetTokenAsync("donor1", UserRole.Donor);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", donorToken);

        // Act
        var response = await _client.GetAsync("/api/logistics-partner/shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAssignedShipments_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act - No authentication header
        var response = await _client.GetAsync("/api/logistics-partner/shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/logistics-partner/shipments/{shipmentId}/location Tests

    [Fact]
    public async Task GetShipmentLocation_WithValidShipmentAndLocation_ReturnsLocation()
    {
        // Arrange
        var (lpToken, lpUserId) = await CreateUserAndGetTokenWithIdAsync("logistics2", UserRole.LogisticsPartner);
        var (coordToken, _) = await CreateUserAndGetTokenWithIdAsync("coordinator2", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient2", UserRole.Recipient);

        var shipmentId = await CreateTestShipmentAsync(recipientId, coordToken);
        await UpdateShipmentToInTransitAsync(shipmentId, coordToken);

        // First update location
        var updateRequest = new UpdateLocationRequest
        {
            Latitude = 40.7128m,
            Longitude = -74.0060m,
            LocationName = "New York City"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);
        await _client.PutAsJsonAsync($"/api/logistics-partner/shipments/{shipmentId}/location", updateRequest);

        // Act - Now retrieve it
        var response = await _client.GetAsync($"/api/logistics-partner/shipments/{shipmentId}/location");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ShipmentLocationDto>();
        result.Should().NotBeNull();
        result!.Latitude.Should().Be(40.7128m);
        result.Longitude.Should().Be(-74.0060m);
        result.LocationName.Should().Be("New York City");
    }

    [Fact]
    public async Task GetShipmentLocation_WithNonExistentShipment_ReturnsNotFound()
    {
        // Arrange
        var lpToken = await CreateUserAndGetTokenAsync("logistics3", UserRole.LogisticsPartner);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.GetAsync("/api/logistics-partner/shipments/nonexistent-id/location");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetShipmentLocation_WithEmptyShipmentId_ReturnsBadRequest()
    {
        // Arrange
        var lpToken = await CreateUserAndGetTokenAsync("logistics4", UserRole.LogisticsPartner);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.GetAsync("/api/logistics-partner/shipments/ /location");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /api/logistics-partner/shipments/{shipmentId}/location Tests

    [Fact]
    public async Task UpdateLocation_WithValidRequest_ReturnsUpdatedLocation()
    {
        // Arrange
        var (lpToken, lpUserId) = await CreateUserAndGetTokenWithIdAsync("logistics5", UserRole.LogisticsPartner);
        var (coordToken, _) = await CreateUserAndGetTokenWithIdAsync("coordinator3", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient3", UserRole.Recipient);

        var shipmentId = await CreateTestShipmentAsync(recipientId, coordToken);
        await UpdateShipmentToInTransitAsync(shipmentId, coordToken);

        var updateRequest = new UpdateLocationRequest
        {
            Latitude = 51.5074m,
            Longitude = -0.1278m,
            LocationName = "London",
            GpsAccuracy = 5.0m
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/logistics-partner/shipments/{shipmentId}/location", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ShipmentLocationDto>();
        result.Should().NotBeNull();
        result!.Latitude.Should().Be(51.5074m);
        result.Longitude.Should().Be(-0.1278m);
        result.LocationName.Should().Be("London");
        result.GpsAccuracy.Should().Be(5.0m);
    }

    [Fact]
    public async Task UpdateLocation_WithInvalidCoordinates_ReturnsBadRequest()
    {
        // Arrange
        var (lpToken, lpUserId) = await CreateUserAndGetTokenWithIdAsync("logistics6", UserRole.LogisticsPartner);
        var (coordToken, _) = await CreateUserAndGetTokenWithIdAsync("coordinator4", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient4", UserRole.Recipient);

        var shipmentId = await CreateTestShipmentAsync(recipientId, coordToken);

        var updateRequest = new UpdateLocationRequest
        {
            Latitude = 95.0m, // Invalid - exceeds 90
            Longitude = -74.0060m,
            LocationName = "Invalid Location"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/logistics-partner/shipments/{shipmentId}/location", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateLocation_WithNonExistentShipment_ReturnsNotFound()
    {
        // Arrange
        var lpToken = await CreateUserAndGetTokenAsync("logistics7", UserRole.LogisticsPartner);

        var updateRequest = new UpdateLocationRequest
        {
            Latitude = 40.7128m,
            Longitude = -74.0060m,
            LocationName = "New York"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.PutAsJsonAsync("/api/logistics-partner/shipments/nonexistent-id/location", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/logistics-partner/shipments/{shipmentId}/delivery-started Tests

    [Fact]
    public async Task ConfirmDeliveryInitiation_WithValidShipment_ReturnsDeliveryEvent()
    {
        // Arrange
        var (lpToken, lpUserId) = await CreateUserAndGetTokenWithIdAsync("logistics8", UserRole.LogisticsPartner);
        var (coordToken, _) = await CreateUserAndGetTokenWithIdAsync("coordinator5", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient5", UserRole.Recipient);

        var shipmentId = await CreateTestShipmentAsync(recipientId, coordToken);
        await UpdateShipmentToInTransitAsync(shipmentId, coordToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.PostAsync($"/api/logistics-partner/shipments/{shipmentId}/delivery-started", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DeliveryEventDto>();
        result.Should().NotBeNull();
        result!.EventType.Should().Be(DeliveryEventType.DeliveryStarted);
        result.ShipmentId.Should().Be(shipmentId);
    }

    [Fact]
    public async Task ConfirmDeliveryInitiation_WithNonExistentShipment_ReturnsNotFound()
    {
        // Arrange
        var lpToken = await CreateUserAndGetTokenAsync("logistics9", UserRole.LogisticsPartner);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.PostAsync("/api/logistics-partner/shipments/nonexistent-id/delivery-started", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/logistics-partner/shipments/{shipmentId}/delivery-history Tests

    [Fact]
    public async Task GetDeliveryHistory_WithExistingEvents_ReturnsEventsList()
    {
        // Arrange
        var (lpToken, lpUserId) = await CreateUserAndGetTokenWithIdAsync("logistics10", UserRole.LogisticsPartner);
        var (coordToken, _) = await CreateUserAndGetTokenWithIdAsync("coordinator6", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient6", UserRole.Recipient);

        var shipmentId = await CreateTestShipmentAsync(recipientId, coordToken);
        await UpdateShipmentToInTransitAsync(shipmentId, coordToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Create some delivery events
        await _client.PostAsync($"/api/logistics-partner/shipments/{shipmentId}/delivery-started", null);

        var updateRequest = new UpdateLocationRequest
        {
            Latitude = 40.7128m,
            Longitude = -74.0060m,
            LocationName = "New York"
        };
        await _client.PutAsJsonAsync($"/api/logistics-partner/shipments/{shipmentId}/location", updateRequest);

        // Act
        var response = await _client.GetAsync($"/api/logistics-partner/shipments/{shipmentId}/delivery-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<DeliveryEventDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCountGreaterThan(0);
        result.Should().Contain(e => e.EventType == DeliveryEventType.DeliveryStarted);
        result.Should().Contain(e => e.EventType == DeliveryEventType.LocationUpdated);
    }

    [Fact]
    public async Task GetDeliveryHistory_WithNonExistentShipment_ReturnsNotFound()
    {
        // Arrange
        var lpToken = await CreateUserAndGetTokenAsync("logistics11", UserRole.LogisticsPartner);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.GetAsync("/api/logistics-partner/shipments/nonexistent-id/delivery-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/logistics-partner/shipments/{shipmentId}/location-history Tests

    [Fact]
    public async Task GetLocationHistory_WithMultipleLocations_ReturnsLocationsList()
    {
        // Arrange
        var (lpToken, lpUserId) = await CreateUserAndGetTokenWithIdAsync("logistics12", UserRole.LogisticsPartner);
        var (coordToken, _) = await CreateUserAndGetTokenWithIdAsync("coordinator7", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient7", UserRole.Recipient);

        var shipmentId = await CreateTestShipmentAsync(recipientId, coordToken);
        await UpdateShipmentToInTransitAsync(shipmentId, coordToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Create multiple location updates
        await _client.PutAsJsonAsync($"/api/logistics-partner/shipments/{shipmentId}/location",
            new UpdateLocationRequest { Latitude = 40.7128m, Longitude = -74.0060m, LocationName = "New York" });

        await _client.PutAsJsonAsync($"/api/logistics-partner/shipments/{shipmentId}/location",
            new UpdateLocationRequest { Latitude = 51.5074m, Longitude = -0.1278m, LocationName = "London" });

        // Act
        var response = await _client.GetAsync($"/api/logistics-partner/shipments/{shipmentId}/location-history?limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ShipmentLocationDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetLocationHistory_WithInvalidLimit_ReturnsBadRequest()
    {
        // Arrange
        var (lpToken, lpUserId) = await CreateUserAndGetTokenWithIdAsync("logistics13", UserRole.LogisticsPartner);
        var (coordToken, _) = await CreateUserAndGetTokenWithIdAsync("coordinator8", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient8", UserRole.Recipient);

        var shipmentId = await CreateTestShipmentAsync(recipientId, coordToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act - limit > 100
        var response = await _client.GetAsync($"/api/logistics-partner/shipments/{shipmentId}/location-history?limit=150");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/logistics-partner/shipments/{shipmentId}/report-issue Tests

    [Fact]
    public async Task ReportDeliveryIssue_WithValidRequest_ReturnsDeliveryEvent()
    {
        // Arrange
        var (lpToken, lpUserId) = await CreateUserAndGetTokenWithIdAsync("logistics14", UserRole.LogisticsPartner);
        var (coordToken, _) = await CreateUserAndGetTokenWithIdAsync("coordinator9", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient9", UserRole.Recipient);

        var shipmentId = await CreateTestShipmentAsync(recipientId, coordToken);
        await UpdateShipmentToInTransitAsync(shipmentId, coordToken);

        var reportRequest = new ReportIssueRequest
        {
            IssueType = IssueType.Delay,
            Description = "Traffic jam on highway",
            Priority = IssuePriority.Medium
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/logistics-partner/shipments/{shipmentId}/report-issue", reportRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DeliveryEventDto>();
        result.Should().NotBeNull();
        result!.EventType.Should().Be(DeliveryEventType.IssueReported);
        result.ShipmentId.Should().Be(shipmentId);
    }

    [Fact]
    public async Task ReportDeliveryIssue_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var (lpToken, lpUserId) = await CreateUserAndGetTokenWithIdAsync("logistics15", UserRole.LogisticsPartner);
        var (coordToken, _) = await CreateUserAndGetTokenWithIdAsync("coordinator10", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient10", UserRole.Recipient);

        var shipmentId = await CreateTestShipmentAsync(recipientId, coordToken);

        // Missing required fields - use invalid description (too short)
        var reportRequest = new ReportIssueRequest
        {
            IssueType = IssueType.Other,
            Description = "Short", // Too short - minimum is 10 characters
            Priority = IssuePriority.Low
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/logistics-partner/shipments/{shipmentId}/report-issue", reportRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/logistics-partner/shipments/{shipmentId}/confirm-receipt Tests

    [Fact]
    public async Task ConfirmReceipt_WithValidShipment_ReturnsDeliveryEvent()
    {
        // Arrange
        var (lpToken, lpUserId) = await CreateUserAndGetTokenWithIdAsync("logistics16", UserRole.LogisticsPartner);
        var (coordToken, _) = await CreateUserAndGetTokenWithIdAsync("coordinator11", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync("recipient11", UserRole.Recipient);

        var shipmentId = await CreateTestShipmentAsync(recipientId, coordToken);
        await UpdateShipmentToInTransitAsync(shipmentId, coordToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.PostAsync($"/api/logistics-partner/shipments/{shipmentId}/confirm-receipt", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DeliveryEventDto>();
        result.Should().NotBeNull();
        result!.EventType.Should().Be(DeliveryEventType.ReceiptConfirmed);
        result.ShipmentId.Should().Be(shipmentId);
    }

    [Fact]
    public async Task ConfirmReceipt_WithNonExistentShipment_ReturnsNotFound()
    {
        // Arrange
        var lpToken = await CreateUserAndGetTokenAsync("logistics17", UserRole.LogisticsPartner);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lpToken);

        // Act
        var response = await _client.PostAsync("/api/logistics-partner/shipments/nonexistent-id/confirm-receipt", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
