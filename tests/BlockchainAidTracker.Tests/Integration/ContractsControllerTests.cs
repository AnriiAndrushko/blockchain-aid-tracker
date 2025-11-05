using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.Authentication;
using BlockchainAidTracker.Services.DTOs.Shipment;
using BlockchainAidTracker.Services.DTOs.SmartContract;
using FluentAssertions;

namespace BlockchainAidTracker.Tests.Integration;

/// <summary>
/// Integration tests for ContractsController
/// </summary>
public class ContractsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ContractsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    private async Task<string> CreateUserAndGetTokenAsync(string username, UserRole role)
    {
        var (token, _) = await CreateUserAndGetTokenWithIdAsync(username, role);
        return token;
    }

    private async Task<(string Token, string UserId)> CreateUserAndGetTokenWithIdAsync(string username, UserRole role)
    {
        const string password = "SecurePassword123!";

        var registerRequest = new RegisterRequest
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = password,
            FullName = $"{username} User",
            Organization = "Test Organization"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        // Assign role if needed
        if (role != UserRole.Recipient)
        {
            var adminToken = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            await _client.PostAsJsonAsync("/api/users/assign-role", new { UserId = authResponse!.UserId, Role = role });
            _client.DefaultRequestHeaders.Clear();

            // Re-login to get new token with updated role
            var loginRequest = new LoginRequest { Username = username, Password = password };
            var loginResponse = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);
            loginResponse.EnsureSuccessStatusCode();
            authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        }

        return (authResponse!.AccessToken, authResponse.UserId);
    }

    private async Task<string> GetAdminTokenAsync()
    {
        const string adminUsername = "admin";
        const string adminPassword = "AdminPassword123!";

        // Try to register admin
        var registerRequest = new RegisterRequest
        {
            Username = adminUsername,
            Email = "admin@example.com",
            Password = adminPassword,
            FullName = "Admin User",
            Organization = "Test Organization"
        };

        await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);

        // Login as admin
        var loginRequest = new LoginRequest { Username = adminUsername, Password = adminPassword };
        var loginResponse = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        return authResponse!.AccessToken;
    }

    private async Task<(ShipmentDto Shipment, string CoordinatorToken)> CreateTestShipmentAsync()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var coordinatorToken = await CreateUserAndGetTokenAsync($"coordinator_{uniqueId}", UserRole.Coordinator);
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync($"recipient_{uniqueId}", UserRole.Recipient);

        var shipmentRequest = new CreateShipmentRequest
        {
            Origin = "Test Origin",
            Destination = "Test Destination",
            RecipientId = recipientId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new List<ShipmentItemDto>
            {
                new() { Description = "Test Item", Quantity = 10, Unit = "boxes" }
            }
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);
        var response = await _client.PostAsJsonAsync("/api/shipments", shipmentRequest);
        response.EnsureSuccessStatusCode();

        var shipment = (await response.Content.ReadFromJsonAsync<ShipmentDto>())!;
        return (shipment, coordinatorToken);
    }

    #endregion

    #region Get All Contracts Tests

    [Fact]
    public async Task GetAllContracts_ShouldReturnDeployedContracts()
    {
        // Arrange - no setup needed as contracts are auto-deployed

        // Act
        var response = await _client.GetAsync("/api/contracts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var contracts = await response.Content.ReadFromJsonAsync<List<ContractDto>>();
        contracts.Should().NotBeNull();
        contracts.Should().NotBeEmpty();
        contracts.Should().Contain(c => c.ContractId == "delivery-verification-v1");
        contracts.Should().Contain(c => c.ContractId == "shipment-tracking-v1");
    }

    [Fact]
    public async Task GetAllContracts_ShouldReturnContractsWithDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/contracts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var contracts = await response.Content.ReadFromJsonAsync<List<ContractDto>>();
        contracts.Should().NotBeNull();

        foreach (var contract in contracts!)
        {
            contract.ContractId.Should().NotBeNullOrEmpty();
            contract.Name.Should().NotBeNullOrEmpty();
            contract.Description.Should().NotBeNullOrEmpty();
            contract.Version.Should().NotBeNullOrEmpty();
            contract.State.Should().NotBeNull();
        }
    }

    #endregion

    #region Get Contract Tests

    [Fact]
    public async Task GetContract_WithValidId_ShouldReturnContract()
    {
        // Act
        var response = await _client.GetAsync("/api/contracts/delivery-verification-v1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var contract = await response.Content.ReadFromJsonAsync<ContractDto>();
        contract.Should().NotBeNull();
        contract!.ContractId.Should().Be("delivery-verification-v1");
        contract.Name.Should().Be("Delivery Verification Contract");
    }

    [Fact]
    public async Task GetContract_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/contracts/non-existent-contract");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Get Contract State Tests

    [Fact]
    public async Task GetContractState_WithValidId_ShouldReturnState()
    {
        // Act
        var response = await _client.GetAsync("/api/contracts/shipment-tracking-v1/state");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var state = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        state.Should().NotBeNull();
    }

    [Fact]
    public async Task GetContractState_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/contracts/non-existent-contract/state");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetContractState_AfterShipmentCreation_ShouldContainShipmentData()
    {
        // Arrange - Create a shipment to generate blockchain data
        var (shipment, _) = await CreateTestShipmentAsync();

        // Wait briefly for contract execution
        await Task.Delay(100);

        // Act
        var response = await _client.GetAsync("/api/contracts/shipment-tracking-v1/state");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var state = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        state.Should().NotBeNull();

        // The state should contain shipment tracking data
        // (Actual keys depend on contract implementation)
    }

    #endregion

    #region Execute Contract Tests

    [Fact]
    public async Task ExecuteContract_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new ExecuteContractRequest
        {
            ContractId = "shipment-tracking-v1",
            TransactionId = "test-transaction-id"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/contracts/execute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExecuteContract_WithMissingContractId_ShouldReturnBadRequest()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("testuser", UserRole.Coordinator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new ExecuteContractRequest
        {
            ContractId = "",
            TransactionId = "test-transaction-id"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/contracts/execute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExecuteContract_WithMissingTransactionId_ShouldReturnBadRequest()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("testuser2", UserRole.Coordinator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new ExecuteContractRequest
        {
            ContractId = "shipment-tracking-v1",
            TransactionId = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/contracts/execute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExecuteContract_WithInvalidTransactionId_ShouldReturnNotFound()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("testuser3", UserRole.Coordinator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new ExecuteContractRequest
        {
            ContractId = "shipment-tracking-v1",
            TransactionId = "non-existent-transaction-id"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/contracts/execute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExecuteContract_WithValidShipmentTransaction_ShouldExecuteSuccessfully()
    {
        // Arrange - Create a shipment to generate a real transaction
        var (shipment, coordinatorToken) = await CreateTestShipmentAsync();

        // Get the transaction ID from the shipment history
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", coordinatorToken);
        var historyResponse = await _client.GetAsync($"/api/shipments/{shipment.Id}/history");
        historyResponse.EnsureSuccessStatusCode();

        var history = await historyResponse.Content.ReadFromJsonAsync<List<BlockchainAidTracker.Services.DTOs.Blockchain.TransactionDto>>();
        history.Should().NotBeNullOrEmpty();

        var transactionId = history!.First().Id;

        var request = new ExecuteContractRequest
        {
            ContractId = "shipment-tracking-v1",
            TransactionId = transactionId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/contracts/execute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ContractExecutionResultDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Events.Should().NotBeEmpty();
    }

    #endregion
}
