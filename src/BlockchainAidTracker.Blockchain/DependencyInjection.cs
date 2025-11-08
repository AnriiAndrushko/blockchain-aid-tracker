using BlockchainAidTracker.Blockchain.Configuration;
using BlockchainAidTracker.Blockchain.Interfaces;
using BlockchainAidTracker.Blockchain.Persistence;
using BlockchainAidTracker.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlockchainAidTracker.Blockchain;

/// <summary>
/// Dependency injection configuration for the blockchain module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds blockchain services to the service collection without persistence.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="hashService">The hash service instance.</param>
    /// <param name="signatureService">The digital signature service instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlockchain(
        this IServiceCollection services,
        IHashService hashService,
        IDigitalSignatureService signatureService)
    {
        var blockchain = new Blockchain(hashService, signatureService);
        services.AddSingleton(blockchain);
        return services;
    }

    /// <summary>
    /// Adds blockchain services with persistence support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="hashService">The hash service instance.</param>
    /// <param name="signatureService">The digital signature service instance.</param>
    /// <param name="persistenceSettings">The persistence configuration settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlockchainWithPersistence(
        this IServiceCollection services,
        IHashService hashService,
        IDigitalSignatureService signatureService,
        BlockchainPersistenceSettings persistenceSettings)
    {
        if (persistenceSettings == null)
        {
            throw new ArgumentNullException(nameof(persistenceSettings));
        }

        // Register persistence settings
        services.AddSingleton(persistenceSettings);

        // Register persistence service if enabled
        if (persistenceSettings.Enabled)
        {
            services.AddSingleton<IBlockchainPersistence>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<JsonBlockchainPersistence>>();
                return new JsonBlockchainPersistence(persistenceSettings, logger);
            });
        }

        // Register blockchain as a singleton with factory
        services.AddSingleton(sp =>
        {
            // Resolve persistence service if it was registered
            var persistence = persistenceSettings.Enabled
                ? sp.GetRequiredService<IBlockchainPersistence>()
                : null;

            return new Blockchain(hashService, signatureService, persistence);
        });

        return services;
    }

    /// <summary>
    /// Loads the blockchain from persistence on startup if configured.
    /// Call this after building the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when loading is finished.</returns>
    public static async Task LoadBlockchainFromPersistenceAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var blockchain = serviceProvider.GetRequiredService<Blockchain>();
        var settings = serviceProvider.GetService<BlockchainPersistenceSettings>();

        if (settings != null && settings.Enabled && settings.AutoLoadOnStartup)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Blockchain>>();

            try
            {
                var loaded = await blockchain.LoadFromPersistenceAsync(cancellationToken);
                if (loaded)
                {
                    logger.LogInformation("Blockchain loaded from persistence successfully");
                }
                else
                {
                    logger.LogInformation("No persisted blockchain data found, starting with fresh chain");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load blockchain from persistence");
                throw;
            }
        }
    }

    /// <summary>
    /// Saves the blockchain to persistence.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when saving is finished.</returns>
    public static async Task SaveBlockchainToPersistenceAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var blockchain = serviceProvider.GetRequiredService<Blockchain>();
        await blockchain.SaveToPersistenceAsync(cancellationToken);
    }
}
