using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.DataAccess.Repositories;
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
}
