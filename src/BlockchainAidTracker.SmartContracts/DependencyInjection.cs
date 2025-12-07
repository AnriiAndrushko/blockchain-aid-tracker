using System.Text.Json;
using BlockchainAidTracker.SmartContracts.Contracts;
using BlockchainAidTracker.SmartContracts.Engine;
using BlockchainAidTracker.SmartContracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BlockchainAidTracker.SmartContracts;

/// <summary>
/// Extension methods for registering smart contract services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds smart contract services to the service collection
    /// </summary>
    public static IServiceCollection AddSmartContracts(this IServiceCollection services)
    {
        // Register the smart contract engine as a singleton
        services.AddSingleton<SmartContractEngine>();

        // Register built-in contracts
        services.AddSingleton<ISmartContract, DeliveryVerificationContract>();
        services.AddSingleton<ISmartContract, ShipmentTrackingContract>();
        services.AddSingleton<ISmartContract, PaymentReleaseContract>();

        return services;
    }

    /// <summary>
    /// Adds smart contract services (call DeployContracts separately after building the service provider)
    /// </summary>
    public static IServiceCollection AddSmartContractsWithAutoDeployment(this IServiceCollection services)
    {
        // Just add the services - deployment will happen in Program.cs after building the app
        services.AddSmartContracts();
        return services;
    }

    /// <summary>
    /// Deploys all registered smart contracts to the engine and persists to database
    /// </summary>
    public static async Task DeployContractsAsync(this IServiceProvider serviceProvider)
    {
        var engine = serviceProvider.GetRequiredService<SmartContractEngine>();
        var contracts = serviceProvider.GetServices<ISmartContract>();

        // Try to get ApplicationDbContext if available (optional - for persistence)
        var dbContextType = Type.GetType("BlockchainAidTracker.DataAccess.ApplicationDbContext, BlockchainAidTracker.DataAccess");
        object? dbContext = null;
        if (dbContextType != null)
        {
            try
            {
                dbContext = serviceProvider.GetService(dbContextType);
            }
            catch
            {
                // DB not available (e.g., in unit tests) - continue without persistence
            }
        }

        foreach (var contract in contracts)
        {
            // Deploy to engine
            var deployed = engine.DeployContract(contract);

            // Save to database if available and newly deployed
            if (deployed && dbContext != null)
            {
                await SaveContractToDatabase(dbContext, contract);
            }
        }
    }

    /// <summary>
    /// Legacy synchronous method - calls async version
    /// </summary>
    public static void DeployContracts(this IServiceProvider serviceProvider)
    {
        DeployContractsAsync(serviceProvider).GetAwaiter().GetResult();
    }

    private static async Task SaveContractToDatabase(object dbContext, ISmartContract contract)
    {
        try
        {
            var dbContextType = dbContext.GetType();
            var smartContractsProperty = dbContextType.GetProperty("SmartContracts");
            if (smartContractsProperty == null)
            {
                Console.WriteLine($"[SmartContract] SmartContracts property not found on DbContext");
                return;
            }

            var smartContractsDbSet = smartContractsProperty.GetValue(dbContext);
            if (smartContractsDbSet == null)
            {
                Console.WriteLine($"[SmartContract] SmartContracts DbSet is null");
                return;
            }

            // Check if contract already exists using Find
            var dbSetType = smartContractsDbSet.GetType();
            var findMethod = dbSetType.GetMethod("Find", new[] { typeof(object[]) });
            if (findMethod != null)
            {
                var existing = findMethod.Invoke(smartContractsDbSet, new object[] { new object[] { contract.ContractId } });
                if (existing != null)
                {
                    Console.WriteLine($"[SmartContract] Contract {contract.ContractId} already exists in DB - skipping");
                    return;
                }
            }

            // Create entity type using reflection
            var entityType = Type.GetType("BlockchainAidTracker.Core.Models.SmartContractEntity, BlockchainAidTracker.Core");
            if (entityType == null)
            {
                Console.WriteLine($"[SmartContract] SmartContractEntity type not found");
                return;
            }

            var state = contract.GetState();
            var deployedAt = state.TryGetValue("_deployedAt", out var deployedAtObj) && deployedAtObj is DateTime dt
                ? dt
                : DateTime.UtcNow;

            var entity = Activator.CreateInstance(entityType);
            if (entity == null)
            {
                Console.WriteLine($"[SmartContract] Failed to create entity instance");
                return;
            }

            // Set properties using reflection
            entityType.GetProperty("Id")?.SetValue(entity, contract.ContractId);
            entityType.GetProperty("Name")?.SetValue(entity, contract.Name);
            entityType.GetProperty("Description")?.SetValue(entity, contract.Description);
            entityType.GetProperty("Version")?.SetValue(entity, contract.Version);
            entityType.GetProperty("Type")?.SetValue(entity, contract.GetType().Name);
            entityType.GetProperty("DeployedAt")?.SetValue(entity, deployedAt);
            entityType.GetProperty("IsEnabled")?.SetValue(entity, true);
            entityType.GetProperty("StateJson")?.SetValue(entity, JsonSerializer.Serialize(state));
            entityType.GetProperty("LastUpdatedAt")?.SetValue(entity, DateTime.UtcNow);

            Console.WriteLine($"[SmartContract] Saving contract {contract.ContractId} to DB with DeployedAt={deployedAt:yyyy-MM-dd HH:mm:ss}");

            // Add to DbSet
            var addMethod = dbSetType.GetMethod("Add", new[] { entityType });
            addMethod?.Invoke(smartContractsDbSet, new[] { entity });

            // Save changes synchronously to ensure it completes
            var saveChangesMethod = dbContextType.GetMethod("SaveChanges", Type.EmptyTypes);
            if (saveChangesMethod != null)
            {
                var result = saveChangesMethod.Invoke(dbContext, null);
                Console.WriteLine($"[SmartContract] Saved contract {contract.ContractId} to DB. Rows affected: {result}");
            }
            else
            {
                Console.WriteLine($"[SmartContract] SaveChanges method not found on DbContext");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SmartContract] ERROR saving contract {contract.ContractId} to DB: {ex.Message}");
            Console.WriteLine($"[SmartContract] Stack trace: {ex.StackTrace}");
        }
    }
}
