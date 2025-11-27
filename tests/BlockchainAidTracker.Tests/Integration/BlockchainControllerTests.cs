using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.Services.DTOs.Authentication;
using BlockchainAidTracker.Services.DTOs.Blockchain;
using BlockchainAidTracker.Services.DTOs.Shipment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BlockchainAidTracker.Tests.Integration;

/// <summary>
/// Integration tests for BlockchainController
/// </summary>
public class BlockchainControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public BlockchainControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a shipment to generate blockchain data for testing
    /// </summary>
    private async Task<ShipmentDto> CreateTestShipmentAsync()
    {
        // Create unique usernames to avoid conflicts when tests run in parallel
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Create coordinator user
        var coordinatorToken = await CreateUserAndGetTokenAsync($"coordinator_{uniqueId}", UserRole.Coordinator);

        // Create recipient user
        var (_, recipientId) = await CreateUserAndGetTokenWithIdAsync($"recipient_{uniqueId}", UserRole.Recipient);

        // Create shipment
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

        return (await response.Content.ReadFromJsonAsync<ShipmentDto>())!;
    }

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

        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = username,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);
        var loginAuthResponse = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        return (loginAuthResponse!.AccessToken, userId);
    }

    #endregion

    #region GET /api/blockchain/chain

    [Fact]
    public async Task GetChain_ShouldReturnCompleteBlockchain()
    {
        // Arrange
        await CreateTestShipmentAsync(); // Creates blockchain data

        // Act
        var response = await _client.GetAsync("/api/blockchain/chain");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var blocks = await response.Content.ReadFromJsonAsync<List<BlockDto>>();
        blocks.Should().NotBeNull();
        blocks.Should().NotBeEmpty();
        blocks![0].Index.Should().Be(0); // Genesis block
    }

    [Fact]
    public async Task GetChain_ShouldIncludeGenesisBlock()
    {
        // Act
        var response = await _client.GetAsync("/api/blockchain/chain");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var blocks = await response.Content.ReadFromJsonAsync<List<BlockDto>>();
        blocks.Should().NotBeNull();
        blocks.Should().HaveCountGreaterThanOrEqualTo(1);

        var genesisBlock = blocks![0];
        genesisBlock.Index.Should().Be(0);
        genesisBlock.PreviousHash.Should().Be("0");
        genesisBlock.ValidatorPublicKey.Should().Be("GENESIS");
    }

    [Fact]
    public async Task GetChain_ShouldIncludeAllTransactions()
    {
        // Arrange
        await CreateTestShipmentAsync();
        await _factory.TriggerBlockCreationAsync();

        // Act
        var response = await _client.GetAsync("/api/blockchain/chain");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var blocks = await response.Content.ReadFromJsonAsync<List<BlockDto>>();
        blocks.Should().NotBeNull();

        // Should have at least genesis block + 1 block with shipment transaction
        blocks.Should().HaveCountGreaterThanOrEqualTo(2);

        // Find a block with transactions
        var blockWithTransactions = blocks!.FirstOrDefault(b => b.Transactions.Count > 0);
        blockWithTransactions.Should().NotBeNull();
        blockWithTransactions!.Transactions.Should().NotBeEmpty();
        blockWithTransactions.Transactions[0].Id.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GET /api/blockchain/blocks/{index}

    [Fact]
    public async Task GetBlockByIndex_WithValidIndex_ShouldReturnBlock()
    {
        // Arrange
        await CreateTestShipmentAsync();

        // Act - Get genesis block
        var response = await _client.GetAsync("/api/blockchain/blocks/0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var block = await response.Content.ReadFromJsonAsync<BlockDto>();
        block.Should().NotBeNull();
        block!.Index.Should().Be(0);
        block.PreviousHash.Should().Be("0");
    }

    [Fact]
    public async Task GetBlockByIndex_WithInvalidIndex_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/blockchain/blocks/9999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBlockByIndex_WithNegativeIndex_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/blockchain/blocks/-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBlockByIndex_ShouldReturnBlockWithAllProperties()
    {
        // Arrange
        await CreateTestShipmentAsync();

        // Act
        var response = await _client.GetAsync("/api/blockchain/blocks/0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var block = await response.Content.ReadFromJsonAsync<BlockDto>();
        block.Should().NotBeNull();
        block!.Index.Should().BeGreaterThanOrEqualTo(0);
        block.Timestamp.Should().NotBe(default(DateTime));
        block.Hash.Should().NotBeNullOrEmpty();
        block.PreviousHash.Should().NotBeNull();
        block.Transactions.Should().NotBeNull();
        block.ValidatorPublicKey.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GET /api/blockchain/transactions/{id}

    [Fact]
    public async Task GetTransactionById_WithValidId_ShouldReturnTransaction()
    {
        // Arrange
        await CreateTestShipmentAsync();
        await _factory.TriggerBlockCreationAsync();

        // Get the chain to find a transaction ID
        var chainResponse = await _client.GetAsync("/api/blockchain/chain");
        var blocks = await chainResponse.Content.ReadFromJsonAsync<List<BlockDto>>();
        var transactionId = blocks!
            .SelectMany(b => b.Transactions)
            .First().Id;

        // Act
        var response = await _client.GetAsync($"/api/blockchain/transactions/{transactionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await response.Content.ReadFromJsonAsync<TransactionDto>();
        transaction.Should().NotBeNull();
        transaction!.Id.Should().Be(transactionId);
        transaction.Type.Should().NotBeNullOrEmpty();
        transaction.SenderPublicKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTransactionById_WithInvalidId_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/blockchain/transactions/invalid-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransactionById_ShouldReturnTransactionWithAllProperties()
    {
        // Arrange
        await CreateTestShipmentAsync();
        await _factory.TriggerBlockCreationAsync();

        // Get a transaction ID from the chain
        var chainResponse = await _client.GetAsync("/api/blockchain/chain");
        var blocks = await chainResponse.Content.ReadFromJsonAsync<List<BlockDto>>();
        var transactionId = blocks!
            .SelectMany(b => b.Transactions)
            .First().Id;

        // Act
        var response = await _client.GetAsync($"/api/blockchain/transactions/{transactionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await response.Content.ReadFromJsonAsync<TransactionDto>();
        transaction.Should().NotBeNull();
        transaction!.Id.Should().NotBeNullOrEmpty();
        transaction.Type.Should().NotBeNullOrEmpty();
        transaction.Timestamp.Should().NotBe(default(DateTime));
        transaction.SenderPublicKey.Should().NotBeNullOrEmpty();
        transaction.PayloadData.Should().NotBeNullOrEmpty();
        transaction.Signature.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region POST /api/blockchain/validate

    [Fact]
    public async Task ValidateChain_WithValidChain_ShouldReturnValidResult()
    {
        // Arrange
        await CreateTestShipmentAsync();

        // Act
        var response = await _client.PostAsync("/api/blockchain/validate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ValidationResultDto>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.BlockCount.Should().BeGreaterThanOrEqualTo(1);
        result.ValidatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateChain_ShouldReturnBlockCount()
    {
        // Arrange
        await CreateTestShipmentAsync();

        // Act
        var response = await _client.PostAsync("/api/blockchain/validate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ValidationResultDto>();
        result.Should().NotBeNull();
        result!.BlockCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ValidateChain_ShouldReturnTimestamp()
    {
        // Act
        var response = await _client.PostAsync("/api/blockchain/validate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ValidationResultDto>();
        result.Should().NotBeNull();
        result!.ValidatedAt.Should().NotBe(default(DateTime));
        result.ValidatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region GET /api/blockchain/pending

    [Fact]
    public async Task GetPendingTransactions_ShouldReturnEmptyList_WhenNoPendingTransactions()
    {
        // Arrange - create a shipment and create a block to clear pending transactions
        await CreateTestShipmentAsync();
        await _factory.TriggerBlockCreationAsync();

        // Act
        var response = await _client.GetAsync("/api/blockchain/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();
        transactions.Should().NotBeNull();
        transactions.Should().BeEmpty(); // No pending transactions after block creation
    }

    [Fact]
    public async Task GetPendingTransactions_ShouldReturnList()
    {
        // Act
        var response = await _client.GetAsync("/api/blockchain/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();
        transactions.Should().NotBeNull();
    }

    #endregion

    #region End-to-End Scenarios

    [Fact]
    public async Task EndToEnd_CreateShipmentAndQueryBlockchain()
    {
        // Arrange & Act - Create shipment (generates blockchain data)
        var shipment = await CreateTestShipmentAsync();
        await _factory.TriggerBlockCreationAsync();

        // Act 1 - Get the complete chain
        var chainResponse = await _client.GetAsync("/api/blockchain/chain");
        chainResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var blocks = await chainResponse.Content.ReadFromJsonAsync<List<BlockDto>>();
        blocks.Should().NotBeNull();
        blocks.Should().HaveCountGreaterThanOrEqualTo(2); // Genesis + at least one block

        // Act 2 - Get a specific block
        var blockResponse = await _client.GetAsync($"/api/blockchain/blocks/{blocks![^1].Index}");
        blockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 3 - Get a specific transaction
        var transactionId = blocks.SelectMany(b => b.Transactions).First().Id;
        var transactionResponse = await _client.GetAsync($"/api/blockchain/transactions/{transactionId}");
        transactionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 4 - Validate the chain
        var validateResponse = await _client.PostAsync("/api/blockchain/validate", null);
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var validationResult = await validateResponse.Content.ReadFromJsonAsync<ValidationResultDto>();
        validationResult!.IsValid.Should().BeTrue();

        // Act 5 - Get pending transactions
        var pendingResponse = await _client.GetAsync("/api/blockchain/pending");
        pendingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
