using BlockchainAidTracker.Services.Configuration;
using BlockchainAidTracker.Services.Consensus;
using BlockchainAidTracker.Services.Interfaces;
using BlockchainAidTracker.Services.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlockchainAidTracker.Services;

/// <summary>
/// Dependency injection configuration for services layer
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds all service layer dependencies to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="jwtSettings">JWT configuration settings</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddServices(this IServiceCollection services, JwtSettings jwtSettings)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (jwtSettings == null)
        {
            throw new ArgumentNullException(nameof(jwtSettings));
        }

        // Register JWT settings as singleton
        services.AddSingleton(jwtSettings);

        // Register transaction signing context as singleton (in-memory key storage)
        services.AddSingleton<TransactionSigningContext>();

        // Register core services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<IKeyManagementService, KeyManagementService>();

        // Register business logic services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IShipmentService, ShipmentService>();
        services.AddScoped<IValidatorService, ValidatorService>();

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
    /// Adds service layer dependencies with blockchain support
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="jwtSettings">JWT configuration settings</param>
    /// <param name="blockchain">Blockchain instance</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddServicesWithBlockchain(
        this IServiceCollection services,
        JwtSettings jwtSettings,
        Blockchain.Blockchain blockchain)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (blockchain == null)
        {
            throw new ArgumentNullException(nameof(blockchain));
        }

        // Register blockchain as singleton
        services.AddSingleton(blockchain);

        // Add all other services
        services.AddServices(jwtSettings);

        return services;
    }
}
