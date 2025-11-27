using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Cryptography;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services;
using BlockchainAidTracker.Services.Consensus;
using BlockchainAidTracker.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlockchainAidTracker.Tests.Integration;

/// <summary>
/// Custom web application factory for integration tests
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"InMemoryTestDb_{Guid.NewGuid()}";
    private bool _isInitialized;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Add DbContext using an in-memory database for testing with a fixed name per factory instance
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Register repositories (since we skipped AddDataAccess in Program.cs for Testing environment)
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IShipmentRepository, ShipmentRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IValidatorRepository, ValidatorRepository>();
        });
    }

    /// <summary>
    /// Initializes test data including validators for block creation
    /// </summary>
    private async Task InitializeTestDataAsync()
    {
        if (_isInitialized)
            return;

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var digitalSignatureService = scope.ServiceProvider.GetRequiredService<IDigitalSignatureService>();
        var keyManagementService = scope.ServiceProvider.GetRequiredService<IKeyManagementService>();

        // Create test validators if none exist
        var validatorCount = await dbContext.Validators.CountAsync();
        if (validatorCount == 0)
        {
            // Create 3 test validators
            for (int i = 0; i < 3; i++)
            {
                var (publicKey, privateKey) = digitalSignatureService.GenerateKeyPair();
                var encryptedPrivateKey = keyManagementService.EncryptPrivateKey(privateKey, "TestValidatorPassword123!");

                var validator = new Validator
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"TestValidator{i + 1}",
                    PublicKey = publicKey,
                    EncryptedPrivateKey = encryptedPrivateKey,
                    Address = $"http://validator{i + 1}:8080",
                    Priority = i,
                    IsActive = true,
                    CreatedTimestamp = DateTime.UtcNow,
                    UpdatedTimestamp = DateTime.UtcNow
                };

                dbContext.Validators.Add(validator);
            }

            await dbContext.SaveChangesAsync();
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Creates an HttpClient and initializes test data
    /// </summary>
    public new HttpClient CreateClient()
    {
        InitializeTestDataAsync().GetAwaiter().GetResult();
        return base.CreateClient();
    }

    /// <summary>
    /// Manually triggers block creation from pending transactions.
    /// This is used in tests since automated block creation is disabled.
    /// </summary>
    public async Task TriggerBlockCreationAsync()
    {
        using var scope = Services.CreateScope();
        var blockchain = scope.ServiceProvider.GetRequiredService<BlockchainAidTracker.Blockchain.Blockchain>();
        var consensusEngine = scope.ServiceProvider.GetRequiredService<IConsensusEngine>();
        var validatorRepository = scope.ServiceProvider.GetRequiredService<IValidatorRepository>();

        // Check if there are pending transactions
        if (blockchain.PendingTransactions.Count == 0)
        {
            return; // No transactions to create a block with
        }

        // Get the next validator
        var nextValidator = await validatorRepository.GetNextValidatorForBlockCreationAsync();
        if (nextValidator == null)
        {
            return; // No validators available
        }

        try
        {
            // Create the block using the consensus engine
            var newBlock = await consensusEngine.CreateBlockAsync(
                blockchain,
                "TestValidatorPassword123!");

            // Add the block to the blockchain
            blockchain.AddBlock(newBlock);
        }
        catch (InvalidOperationException)
        {
            // Block creation failed, which is ok in tests (e.g., no transactions, no validators)
        }
    }
}
