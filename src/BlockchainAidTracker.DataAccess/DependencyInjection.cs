using BlockchainAidTracker.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlockchainAidTracker.DataAccess;

/// <summary>
/// Dependency injection configuration for the DataAccess layer
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds DataAccess layer services to the dependency injection container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext with SQLite
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=blockchain-aid-tracker.db";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IValidatorRepository, ValidatorRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }

    /// <summary>
    /// Adds DataAccess layer services with PostgreSQL
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDataAccessWithPostgreSQL(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext with PostgreSQL
        var connectionString = configuration.GetConnectionString("PostgreSQLConnection")
            ?? throw new InvalidOperationException("PostgreSQL connection string not found");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IValidatorRepository, ValidatorRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }

    /// <summary>
    /// Adds DataAccess layer services with in-memory database (for testing)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="databaseName">Database name</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDataAccessWithInMemoryDatabase(
        this IServiceCollection services,
        string databaseName = "TestDatabase")
    {
        // Register DbContext with in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IValidatorRepository, ValidatorRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }
}
