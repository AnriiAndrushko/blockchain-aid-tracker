using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.Services.DTOs.Authentication;
using BlockchainAidTracker.Services.DTOs.Consensus;
using BlockchainAidTracker.Services.DTOs.Shipment;
using BlockchainAidTracker.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BlockchainAidTracker.Tests.Integration;

/// <summary>
/// Integration tests for ConsensusController
/// </summary>
public class ConsensusControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ConsensusControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    private async Task<string> CreateUserAndGetTokenAsync(string username, UserRole role)
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
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        return loginResult!.AccessToken;
    }

    private async Task<Validator> CreateTestValidatorAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var signatureService = scope.ServiceProvider.GetRequiredService<IDigitalSignatureService>();
        var keyManagementService = scope.ServiceProvider.GetRequiredService<IKeyManagementService>();

        var (privateKey, publicKey) = signatureService.GenerateKeyPair();
        var encryptedPrivateKey = keyManagementService.EncryptPrivateKey(privateKey, "TestValidatorPassword123!");

        var validator = new Validator
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"Test Validator {Guid.NewGuid().ToString("N")[..8]}",
            PublicKey = publicKey,
            EncryptedPrivateKey = encryptedPrivateKey,
            Address = "http://localhost:5000",
            Priority = 1,
            IsActive = true,
            TotalBlocksCreated = 0,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        await dbContext.Validators.AddAsync(validator);
        await dbContext.SaveChangesAsync();

        return validator;
    }

    private async Task CreatePendingTransactionAsync()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Create coordinator user
        var coordinatorToken = await CreateUserAndGetTokenAsync($"coordinator_{uniqueId}", UserRole.Coordinator);

        // Create recipient user
        var recipientToken = await CreateUserAndGetTokenAsync($"recipient_{uniqueId}", UserRole.Recipient);

        // Get recipient ID
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);
        var profileResponse = await _client.GetAsync("/api/users/profile");
        var profile = await profileResponse.Content.ReadFromJsonAsync<dynamic>();
        var recipientId = profile!.GetProperty("id").GetString()!;

        // Create shipment (this adds a transaction)
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
    }

    #endregion

    #region GetConsensusStatus Tests

    [Fact]
    public async Task GetConsensusStatus_ReturnsOk_WithCorrectStatusInformation()
    {
        // Arrange
        var validator = await CreateTestValidatorAsync();

        // Act
        var response = await _client.GetAsync("/api/consensus/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await response.Content.ReadFromJsonAsync<ConsensusStatusDto>();
        status.Should().NotBeNull();
        status!.ChainHeight.Should().BeGreaterThanOrEqualTo(1); // At least genesis block
        status.PendingTransactionCount.Should().BeGreaterThanOrEqualTo(0);
        status.ActiveValidatorCount.Should().BeGreaterThanOrEqualTo(1);
        status.LastBlockHash.Should().NotBeNullOrEmpty();
        status.LastBlockTimestamp.Should().NotBeNull();
    }

    [Fact]
    public async Task GetConsensusStatus_WithPendingTransactions_ReflectsCorrectCount()
    {
        // Arrange
        await CreateTestValidatorAsync();

        // Directly add a transaction to the blockchain
        using var scope = _factory.Services.CreateScope();
        var blockchain = scope.ServiceProvider.GetRequiredService<Blockchain.Blockchain>();

        var testTransaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = "test-key",
            Payload = "test-payload",
            Signature = "test-signature"
        };
        blockchain.AddTransaction(testTransaction);

        // Act
        var response = await _client.GetAsync("/api/consensus/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await response.Content.ReadFromJsonAsync<ConsensusStatusDto>();
        status.Should().NotBeNull();
        status!.PendingTransactionCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region CreateBlock Tests

    [Fact]
    public async Task CreateBlock_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateBlockRequest
        {
            ValidatorPassword = "TestValidatorPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/consensus/create-block", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateBlock_WithRecipientRole_ReturnsForbidden()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var recipientToken = await CreateUserAndGetTokenAsync($"recipient_{uniqueId}", UserRole.Recipient);

        var request = new CreateBlockRequest
        {
            ValidatorPassword = "TestValidatorPassword123!"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/consensus/create-block", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateBlock_WithNoPendingTransactions_ReturnsBadRequest()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await CreateUserAndGetTokenAsync($"admin_{uniqueId}", UserRole.Administrator);
        await CreateTestValidatorAsync();

        var request = new CreateBlockRequest
        {
            ValidatorPassword = "TestValidatorPassword123!"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/consensus/create-block", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("No Pending Transactions");
    }

    [Fact]
    public async Task CreateBlock_WithNoActiveValidators_ReturnsBadRequest()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await CreateUserAndGetTokenAsync($"admin_{uniqueId}", UserRole.Administrator);

        // Directly add a transaction to the blockchain to ensure there are pending transactions
        using var scope = _factory.Services.CreateScope();
        var blockchain = scope.ServiceProvider.GetRequiredService<Blockchain.Blockchain>();

        var testTransaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = "test-key",
            Payload = "test-payload",
            Signature = "test-signature"
        };
        blockchain.AddTransaction(testTransaction);

        var request = new CreateBlockRequest
        {
            ValidatorPassword = "TestValidatorPassword123!"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/consensus/create-block", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("No Active Validators");
    }

    [Fact]
    public async Task CreateBlock_WithValidRequest_CreatesBlockSuccessfully()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await CreateUserAndGetTokenAsync($"admin_{uniqueId}", UserRole.Administrator);
        await CreateTestValidatorAsync();

        // Directly add a transaction to the blockchain
        using var scope = _factory.Services.CreateScope();
        var blockchain = scope.ServiceProvider.GetRequiredService<Blockchain.Blockchain>();

        var testTransaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = "test-key",
            Payload = "test-payload",
            Signature = "test-signature"
        };
        blockchain.AddTransaction(testTransaction);

        var request = new CreateBlockRequest
        {
            ValidatorPassword = "TestValidatorPassword123!"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/consensus/create-block", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BlockCreationResultDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Block.Should().NotBeNull();
        result.ValidatorId.Should().NotBeNullOrEmpty();
        result.ValidatorName.Should().NotBeNullOrEmpty();
        result.TransactionCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateBlock_WithValidatorRole_CreatesBlockSuccessfully()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var validatorUserToken = await CreateUserAndGetTokenAsync($"validator_{uniqueId}", UserRole.Validator);
        await CreateTestValidatorAsync();

        // Directly add a transaction to the blockchain
        using var scope = _factory.Services.CreateScope();
        var blockchain = scope.ServiceProvider.GetRequiredService<Blockchain.Blockchain>();

        var testTransaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = TransactionType.ShipmentCreated,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = "test-key",
            Payload = "test-payload",
            Signature = "test-signature"
        };
        blockchain.AddTransaction(testTransaction);

        var request = new CreateBlockRequest
        {
            ValidatorPassword = "TestValidatorPassword123!"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validatorUserToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/consensus/create-block", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BlockCreationResultDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    #endregion

    #region ValidateBlock Tests

    [Fact]
    public async Task ValidateBlock_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/consensus/validate-block/0", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidateBlock_WithRecipientRole_ReturnsForbidden()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var recipientToken = await CreateUserAndGetTokenAsync($"recipient_{uniqueId}", UserRole.Recipient);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);

        // Act
        var response = await _client.PostAsync("/api/consensus/validate-block/0", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ValidateBlock_GenesisBlock_ReturnsValidTrue()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await CreateUserAndGetTokenAsync($"admin_{uniqueId}", UserRole.Administrator);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsync("/api/consensus/validate-block/0", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        result.Should().NotBeNull();
        result!.GetProperty("isValid").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ValidateBlock_NonExistentBlock_ReturnsNotFound()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await CreateUserAndGetTokenAsync($"admin_{uniqueId}", UserRole.Administrator);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PostAsync("/api/consensus/validate-block/999", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetActiveValidators Tests

    [Fact]
    public async Task GetActiveValidators_ReturnsOk_WithValidatorList()
    {
        // Arrange
        await CreateTestValidatorAsync();

        // Act
        var response = await _client.GetAsync("/api/consensus/validators");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var validators = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        validators.Should().NotBeNull();
        validators!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetActiveValidators_WithMultipleValidators_ReturnsAllActive()
    {
        // Arrange
        await CreateTestValidatorAsync();
        await CreateTestValidatorAsync();
        await CreateTestValidatorAsync();

        // Act
        var response = await _client.GetAsync("/api/consensus/validators");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var validators = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        validators.Should().NotBeNull();
        validators!.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    #endregion
}
