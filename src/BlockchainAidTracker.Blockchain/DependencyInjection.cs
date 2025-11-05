using BlockchainAidTracker.Blockchain.Consensus;
using Microsoft.Extensions.DependencyInjection;

namespace BlockchainAidTracker.Blockchain;

/// <summary>
/// Extension methods for registering blockchain services with dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers blockchain services including the core blockchain engine.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlockchain(this IServiceCollection services)
    {
        // Register the blockchain as a singleton so all services share the same chain
        services.AddSingleton<Blockchain>();

        return services;
    }

    /// <summary>
    /// Registers the Proof-of-Authority consensus engine.
    /// Requires that IValidatorRepository, IKeyManagementService, IDigitalSignatureService,
    /// and IHashService are already registered.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddProofOfAuthorityConsensus(this IServiceCollection services)
    {
        services.AddScoped<IConsensusEngine, ProofOfAuthorityConsensusEngine>();

        return services;
    }

    /// <summary>
    /// Registers blockchain services along with the Proof-of-Authority consensus engine.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlockchainWithPoAConsensus(this IServiceCollection services)
    {
        services.AddBlockchain();
        services.AddProofOfAuthorityConsensus();

        return services;
    }
}
