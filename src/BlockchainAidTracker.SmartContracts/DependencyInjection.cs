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

        return services;
    }

    /// <summary>
    /// Adds smart contract services and automatically deploys registered contracts
    /// </summary>
    public static IServiceCollection AddSmartContractsWithAutoDeployment(this IServiceCollection services)
    {
        services.AddSmartContracts();

        // Register a hosted service or startup action to deploy contracts
        services.AddSingleton(sp =>
        {
            var engine = sp.GetRequiredService<SmartContractEngine>();
            var contracts = sp.GetServices<ISmartContract>();

            foreach (var contract in contracts)
            {
                engine.DeployContract(contract);
            }

            return engine;
        });

        return services;
    }
}
