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
    /// Deploys all registered smart contracts to the engine
    /// </summary>
    public static void DeployContracts(this IServiceProvider serviceProvider)
    {
        var engine = serviceProvider.GetRequiredService<SmartContractEngine>();
        var contracts = serviceProvider.GetServices<ISmartContract>();

        foreach (var contract in contracts)
        {
            engine.DeployContract(contract);
        }
    }
}
