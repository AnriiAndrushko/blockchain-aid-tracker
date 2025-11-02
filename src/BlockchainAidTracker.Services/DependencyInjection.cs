using BlockchainAidTracker.Services.Configuration;
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

        // Register core services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IQrCodeService, QrCodeService>();

        // Register business logic services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IShipmentService, ShipmentService>();

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
