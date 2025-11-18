using System.Net;
using System.Net.Http.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.Services.DTOs.Authentication;
using BlockchainAidTracker.Services.DTOs.Shipment;
using BlockchainAidTracker.Services.DTOs.Blockchain;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlockchainAidTracker.Tests.Integration;

/// <summary>
/// Comprehensive integration tests for complete shipment pipeline with all user roles
/// </summary>
public class FullShipmentPipelineTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string _testRunId;

    public FullShipmentPipelineTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _testRunId = Guid.NewGuid().ToString("N")[..8];
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean up test database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task FullShipmentPipeline_WithAllUserRoles_ShouldCompleteSuccessfully()
    {
        // Arrange - Create all 6 user types
        var (adminToken, adminId) = await CreateUserAsync("administrator", UserRole.Administrator, "Admin", "User");
        var (coordinatorToken, coordinatorId) = await CreateUserAsync("coordinator", UserRole.Coordinator, "Alice", "Coordinator");
        var (logisticsToken, logisticsId) = await CreateUserAsync("logistics", UserRole.LogisticsPartner, "Bob", "LogisticsPartner");
        var (recipientToken, recipientId) = await CreateUserAsync("recipient", UserRole.Recipient, "Carol", "Recipient");
        var (donorToken, donorId) = await CreateUserAsync("donor", UserRole.Donor, "David", "Donor");
        var (validatorToken, validatorId) = await CreateUserAsync("validator", UserRole.Validator, "Eve", "Validator");

        // Verify all users were created
        Assert.NotNull(adminToken);
        Assert.NotNull(coordinatorToken);
        Assert.NotNull(logisticsToken);
        Assert.NotNull(recipientToken);
        Assert.NotNull(donorToken);
        Assert.NotNull(validatorToken);

        // Act & Assert - Step 1: Coordinator creates a shipment
        var createRequest = new CreateShipmentRequest
        {
            Origin = "Warehouse A - New York",
            Destination = "Clinic B - Kenya",
            RecipientId = recipientId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new List<ShipmentItemDto>
            {
                new() { Description = "Medical Supplies", Quantity = 100, Unit = "boxes" },
                new() { Description = "Water Purification Tablets", Quantity = 500, Unit = "packets" },
                new() { Description = "Emergency Food Rations", Quantity = 200, Unit = "packs" }
            }
        };

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", coordinatorToken);
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        Assert.NotNull(createdShipment);
        Assert.Equal(ShipmentStatus.Created, createdShipment.Status);
        Assert.Equal(3, createdShipment.Items.Count);
        Assert.Single(createdShipment.BlockchainTransactionIds); // SHIPMENT_CREATED transaction

        var shipmentId = createdShipment.Id;

        // Assert - Donor can view the shipment (read-only transparency)
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", donorToken);
        var donorViewResponse = await _client.GetAsync($"/api/shipments/{shipmentId}");
        Assert.Equal(HttpStatusCode.OK, donorViewResponse.StatusCode);
        var donorViewedShipment = await donorViewResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        Assert.NotNull(donorViewedShipment);
        Assert.Equal(shipmentId, donorViewedShipment.Id);

        // Assert - Donor cannot update shipment status (should fail with 403)
        var donorUpdateRequest = new { Status = ShipmentStatus.Validated };
        var donorUpdateResponse = await _client.PutAsJsonAsync($"/api/shipments/{shipmentId}/status", donorUpdateRequest);
        Assert.Equal(HttpStatusCode.Forbidden, donorUpdateResponse.StatusCode);

        // Act & Assert - Step 2: LogisticsPartner updates status to Validated (quality check)
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", logisticsToken);
        var validateRequest = new { Status = ShipmentStatus.Validated };
        var validateResponse = await _client.PutAsJsonAsync($"/api/shipments/{shipmentId}/status", validateRequest);
        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);

        var validatedShipment = await validateResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        Assert.NotNull(validatedShipment);
        Assert.Equal(ShipmentStatus.Validated, validatedShipment.Status);
        Assert.Equal(2, validatedShipment.BlockchainTransactionIds.Count); // SHIPMENT_CREATED + STATUS_UPDATED

        // Act & Assert - Step 3: LogisticsPartner marks as InTransit (begins delivery)
        var inTransitRequest = new { Status = ShipmentStatus.InTransit };
        var inTransitResponse = await _client.PutAsJsonAsync($"/api/shipments/{shipmentId}/status", inTransitRequest);
        Assert.Equal(HttpStatusCode.OK, inTransitResponse.StatusCode);

        var inTransitShipment = await inTransitResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        Assert.NotNull(inTransitShipment);
        Assert.Equal(ShipmentStatus.InTransit, inTransitShipment.Status);
        Assert.Equal(3, inTransitShipment.BlockchainTransactionIds.Count);

        // Act & Assert - Step 4: LogisticsPartner marks as Delivered (arrives at destination)
        var deliveredRequest = new { Status = ShipmentStatus.Delivered };
        var deliveredResponse = await _client.PutAsJsonAsync($"/api/shipments/{shipmentId}/status", deliveredRequest);
        Assert.Equal(HttpStatusCode.OK, deliveredResponse.StatusCode);

        var deliveredShipment = await deliveredResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        Assert.NotNull(deliveredShipment);
        Assert.Equal(ShipmentStatus.Delivered, deliveredShipment.Status);
        Assert.Equal(4, deliveredShipment.BlockchainTransactionIds.Count);

        // Act & Assert - Step 5: Recipient confirms delivery
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", recipientToken);
        var confirmResponse = await _client.PostAsJsonAsync($"/api/shipments/{shipmentId}/confirm-delivery", new { });
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var confirmedShipment = await confirmResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        Assert.NotNull(confirmedShipment);
        Assert.Equal(ShipmentStatus.Confirmed, confirmedShipment.Status);
        Assert.Equal(5, confirmedShipment.BlockchainTransactionIds.Count); // All 5 transactions

        // Assert - Verify blockchain integrity
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        var chainResponse = await _client.GetAsync("/api/blockchain/chain");
        Assert.Equal(HttpStatusCode.OK, chainResponse.StatusCode);

        var blocks = await chainResponse.Content.ReadFromJsonAsync<List<BlockDto>>();
        Assert.NotNull(blocks);
        Assert.True(blocks.Count > 1); // Genesis block + at least one block with transactions

        // Validate blockchain
        var validateChainResponse = await _client.PostAsJsonAsync("/api/blockchain/validate", new { });
        Assert.Equal(HttpStatusCode.OK, validateChainResponse.StatusCode);

        var validationResult = await validateChainResponse.Content.ReadFromJsonAsync<ValidationResultDto>();
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);

        // Assert - Verify all blockchain transactions exist
        var shipmentHistoryResponse = await _client.GetAsync($"/api/shipments/{shipmentId}/history");
        Assert.Equal(HttpStatusCode.OK, shipmentHistoryResponse.StatusCode);

        var transactions = await shipmentHistoryResponse.Content.ReadFromJsonAsync<List<TransactionDto>>();
        Assert.NotNull(transactions);
        Assert.Equal(5, transactions.Count);

        // Verify transaction types in order
        Assert.Equal(TransactionType.ShipmentCreated, transactions[0].Type);
        Assert.Equal(TransactionType.StatusUpdated, transactions[1].Type); // Created -> Validated
        Assert.Equal(TransactionType.StatusUpdated, transactions[2].Type); // Validated -> InTransit
        Assert.Equal(TransactionType.StatusUpdated, transactions[3].Type); // InTransit -> Delivered
        Assert.Equal(TransactionType.DeliveryConfirmed, transactions[4].Type); // Final confirmation

        // All transactions should be signed
        Assert.All(transactions, t => Assert.NotEmpty(t.Signature));
    }

    [Fact]
    public async Task ShipmentStatusUpdate_RecipientRole_ShouldBeForbidden()
    {
        // Arrange
        var (coordinatorToken, coordinatorId) = await CreateUserAsync("coordinator2", UserRole.Coordinator, "Alice", "Coordinator");
        var (recipientToken, recipientId) = await CreateUserAsync("recipient2", UserRole.Recipient, "Carol", "Recipient");

        // Create shipment as coordinator
        var createRequest = new CreateShipmentRequest
        {
            Origin = "Warehouse C",
            Destination = "Clinic D",
            RecipientId = recipientId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new List<ShipmentItemDto>
            {
                new() { Description = "Test Supplies", Quantity = 10, Unit = "boxes" }
            }
        };

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", coordinatorToken);
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>();

        // Act - Recipient tries to update status
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", recipientToken);
        var updateRequest = new { Status = ShipmentStatus.InTransit };
        var updateResponse = await _client.PutAsJsonAsync($"/api/shipments/{createdShipment!.Id}/status", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task ShipmentStatusUpdate_DonorRole_ShouldBeForbidden()
    {
        // Arrange
        var (coordinatorToken, coordinatorId) = await CreateUserAsync("coordinator3", UserRole.Coordinator, "Alice", "Coordinator");
        var (donorToken, donorId) = await CreateUserAsync("donor3", UserRole.Donor, "David", "Donor");
        var (recipientToken, recipientId) = await CreateUserAsync("recipient3", UserRole.Recipient, "Carol", "Recipient");

        // Create shipment as coordinator
        var createRequest = new CreateShipmentRequest
        {
            Origin = "Warehouse E",
            Destination = "Clinic F",
            RecipientId = recipientId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new List<ShipmentItemDto>
            {
                new() { Description = "Test Supplies", Quantity = 10, Unit = "boxes" }
            }
        };

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", coordinatorToken);
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>();

        // Act - Donor tries to update status
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", donorToken);
        var updateRequest = new { Status = ShipmentStatus.InTransit };
        var updateResponse = await _client.PutAsJsonAsync($"/api/shipments/{createdShipment!.Id}/status", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task ShipmentCreation_LogisticsPartnerRole_ShouldBeForbidden()
    {
        // Arrange
        var (logisticsToken, logisticsId) = await CreateUserAsync("logistics4", UserRole.LogisticsPartner, "Bob", "LogisticsPartner");
        var (recipientToken, recipientId) = await CreateUserAsync("recipient4", UserRole.Recipient, "Carol", "Recipient");

        var createRequest = new CreateShipmentRequest
        {
            Origin = "Warehouse G",
            Destination = "Clinic H",
            RecipientId = recipientId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new List<ShipmentItemDto>
            {
                new() { Description = "Test Supplies", Quantity = 10, Unit = "boxes" }
            }
        };

        // Act - LogisticsPartner tries to create shipment
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", logisticsToken);
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }

    [Fact]
    public async Task AllRoles_CanViewShipments_Successfully()
    {
        // Arrange - Create all user types
        var (adminToken, _) = await CreateUserAsync("admin5", UserRole.Administrator, "Admin", "User");
        var (coordinatorToken, coordinatorId) = await CreateUserAsync("coordinator5", UserRole.Coordinator, "Alice", "Coordinator");
        var (logisticsToken, _) = await CreateUserAsync("logistics5", UserRole.LogisticsPartner, "Bob", "LogisticsPartner");
        var (recipientToken, recipientId) = await CreateUserAsync("recipient5", UserRole.Recipient, "Carol", "Recipient");
        var (donorToken, _) = await CreateUserAsync("donor5", UserRole.Donor, "David", "Donor");
        var (validatorToken, _) = await CreateUserAsync("validator5", UserRole.Validator, "Eve", "Validator");

        // Create a shipment
        var createRequest = new CreateShipmentRequest
        {
            Origin = "Warehouse I",
            Destination = "Clinic J",
            RecipientId = recipientId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new List<ShipmentItemDto>
            {
                new() { Description = "Test Supplies", Quantity = 10, Unit = "boxes" }
            }
        };

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", coordinatorToken);
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>();
        Assert.NotNull(createdShipment);

        // Act & Assert - All roles can view shipments
        var roles = new[]
        {
            ("Administrator", adminToken),
            ("Coordinator", coordinatorToken),
            ("LogisticsPartner", logisticsToken),
            ("Recipient", recipientToken),
            ("Donor", donorToken),
            ("Validator", validatorToken)
        };

        foreach (var (roleName, token) in roles)
        {
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var viewResponse = await _client.GetAsync($"/api/shipments/{createdShipment.Id}");

            Assert.Equal(HttpStatusCode.OK, viewResponse.StatusCode);
            var shipment = await viewResponse.Content.ReadFromJsonAsync<ShipmentDto>();
            Assert.NotNull(shipment);
            Assert.Equal(createdShipment.Id, shipment.Id);
        }
    }

    /// <summary>
    /// Helper method to create a user and return their JWT token and user ID
    /// </summary>
    private async Task<(string token, string userId)> CreateUserAsync(string username, UserRole role, string firstName, string lastName)
    {
        var uniqueUsername = $"{username}_{_testRunId}";
        var uniqueEmail = $"{username}_{_testRunId}@test.com";

        var registerRequest = new RegisterRequest
        {
            Username = uniqueUsername,
            Email = uniqueEmail,
            Password = "Test123!@#",
            FirstName = firstName,
            LastName = lastName,
            Organization = $"{role} Organization"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authResponse);

        var userId = authResponse.UserId;
        var token = authResponse.AccessToken;

        // If not Recipient (default role), update role using admin privileges
        if (role != UserRole.Recipient)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FindAsync(userId);
            Assert.NotNull(user);

            user.Role = role;
            await dbContext.SaveChangesAsync();
        }

        return (token, userId);
    }
}
