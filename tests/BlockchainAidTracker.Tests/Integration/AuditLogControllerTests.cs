using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.AuditLog;
using BlockchainAidTracker.Services.DTOs.Authentication;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BlockchainAidTracker.Tests.Integration;

/// <summary>
/// Integration tests for AuditLogController
/// </summary>
public class AuditLogControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuditLogControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    private async Task<string> CreateAdminUserAndGetTokenAsync()
    {
        var registerRequest = new RegisterRequest
        {
            Username = "adminuser",
            Email = "admin@test.com",
            Password = "AdminPass123!",
            FullName = "Admin User",
            Organization = "Test Organization"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/authentication/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        var userId = authResponse!.UserId;

        // Update role to Administrator using direct database access
        using var scope = _factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<BlockchainAidTracker.DataAccess.Repositories.IUserRepository>();
        var user = await userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            user.Role = UserRole.Administrator;
            userRepository.Update(user);
        }

        // Login to get token with Administrator role
        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "adminuser",
            Password = "AdminPass123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);
        var loginAuthResponse = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        return loginAuthResponse!.AccessToken;
    }

    private async Task CreateSampleAuditLogsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var auditLogRepository = scope.ServiceProvider.GetRequiredService<BlockchainAidTracker.DataAccess.Repositories.IAuditLogRepository>();

        var logs = new[]
        {
            AuditLog.Success(AuditLogCategory.Authentication, AuditLogAction.UserLoggedIn, "User logged in", "user-1", "user1"),
            AuditLog.Success(AuditLogCategory.Shipment, AuditLogAction.ShipmentCreated, "Shipment created", "user-2", entityId: "shipment-1"),
            AuditLog.Failure(AuditLogCategory.Blockchain, AuditLogAction.BlockCreated, "Block creation failed", "Insufficient validators"),
            AuditLog.Success(AuditLogCategory.UserManagement, AuditLogAction.UserActivated, "User activated", "admin-1", entityId: "user-3"),
            AuditLog.Success(AuditLogCategory.Validator, AuditLogAction.ValidatorRegistered, "Validator registered", entityId: "validator-1")
        };

        foreach (var log in logs)
        {
            await auditLogRepository.AddAsync(log);
        }
    }

    #endregion

    #region GET /api/audit-logs

    [Fact]
    public async Task GetRecentLogs_WithAdminToken_ReturnsOk()
    {
        // Arrange
        var token = await CreateAdminUserAndGetTokenAsync();
        await CreateSampleAuditLogsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit-logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLogDto>>();
        logs.Should().NotBeNull();
        logs!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRecentLogs_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/audit-logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRecentLogs_WithPagination_ReturnsCorrectPageSize()
    {
        // Arrange
        var token = await CreateAdminUserAndGetTokenAsync();
        await CreateSampleAuditLogsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit-logs?pageSize=2&pageNumber=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLogDto>>();
        logs.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetRecentLogs_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Arrange
        var token = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit-logs?pageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/audit-logs/filter

    [Fact]
    public async Task GetFilteredLogs_WithValidFilter_ReturnsFilteredLogs()
    {
        // Arrange
        var token = await CreateAdminUserAndGetTokenAsync();
        await CreateSampleAuditLogsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var filter = new AuditLogFilterRequest
        {
            Category = AuditLogCategory.Shipment,
            PageSize = 50,
            PageNumber = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/audit-logs/filter", filter);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLogDto>>();
        logs.Should().NotBeNull();
        logs!.Should().AllSatisfy(log => log.Category.Should().Be(AuditLogCategory.Shipment));
    }

    [Fact]
    public async Task GetFilteredLogs_WithSuccessFilter_ReturnsOnlySuccessfulLogs()
    {
        // Arrange
        var token = await CreateAdminUserAndGetTokenAsync();
        await CreateSampleAuditLogsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var filter = new AuditLogFilterRequest
        {
            IsSuccess = true,
            PageSize = 50,
            PageNumber = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/audit-logs/filter", filter);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLogDto>>();
        logs.Should().NotBeNull();
        logs!.Should().AllSatisfy(log => log.IsSuccess.Should().BeTrue());
    }

    #endregion

    #region GET /api/audit-logs/category/{category}

    [Fact]
    public async Task GetLogsByCategory_WithValidCategory_ReturnsLogsInCategory()
    {
        // Arrange
        var token = await CreateAdminUserAndGetTokenAsync();
        await CreateSampleAuditLogsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/audit-logs/category/{AuditLogCategory.Authentication}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLogDto>>();
        logs.Should().NotBeNull();
        logs!.Should().AllSatisfy(log => log.Category.Should().Be(AuditLogCategory.Authentication));
    }

    #endregion

    #region GET /api/audit-logs/user/{userId}

    [Fact]
    public async Task GetLogsByUser_WithValidUserId_ReturnsUserLogs()
    {
        // Arrange
        var token = await CreateAdminUserAndGetTokenAsync();
        await CreateSampleAuditLogsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit-logs/user/user-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLogDto>>();
        logs.Should().NotBeNull();
        logs!.Should().AllSatisfy(log => log.UserId.Should().Be("user-1"));
    }

    [Fact]
    public async Task GetLogsByUser_WithEmptyUserId_ReturnsBadRequest()
    {
        // Arrange
        var token = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit-logs/user/ ");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/audit-logs/entity/{entityId}

    [Fact]
    public async Task GetLogsByEntity_WithValidEntityId_ReturnsEntityLogs()
    {
        // Arrange
        var token = await CreateAdminUserAndGetTokenAsync();
        await CreateSampleAuditLogsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit-logs/entity/shipment-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLogDto>>();
        logs.Should().NotBeNull();
        logs!.Should().AllSatisfy(log => log.EntityId.Should().Be("shipment-1"));
    }

    #endregion

    #region GET /api/audit-logs/failed

    [Fact]
    public async Task GetFailedLogs_ReturnsOnlyFailedLogs()
    {
        // Arrange
        var token = await CreateAdminUserAndGetTokenAsync();
        await CreateSampleAuditLogsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit-logs/failed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLogDto>>();
        logs.Should().NotBeNull();
        logs!.Should().AllSatisfy(log => log.IsSuccess.Should().BeFalse());
    }

    #endregion

    #region GET /api/audit-logs/category/{category}/count

    [Fact]
    public async Task GetCountByCategory_WithValidCategory_ReturnsCorrectCount()
    {
        // Arrange
        var token = await CreateAdminUserAndGetTokenAsync();
        await CreateSampleAuditLogsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/audit-logs/category/{AuditLogCategory.Authentication}/count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await response.Content.ReadFromJsonAsync<int>();
        count.Should().BeGreaterThanOrEqualTo(1);
    }

    #endregion
}
