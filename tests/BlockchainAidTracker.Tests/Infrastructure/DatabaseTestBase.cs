using BlockchainAidTracker.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BlockchainAidTracker.Tests.Infrastructure;

/// <summary>
/// Base class for database integration tests.
/// Provides isolated in-memory database for each test with automatic cleanup.
/// </summary>
public abstract class DatabaseTestBase : IDisposable
{
    protected ApplicationDbContext Context { get; private set; }
    private readonly string _databaseName;

    protected DatabaseTestBase()
    {
        // Create unique database name for test isolation
        _databaseName = $"TestDb_{Guid.NewGuid()}";

        // Create in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        Context = new ApplicationDbContext(options);

        // Ensure database is created
        Context.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a new DbContext instance with the same database
    /// Useful for testing scenarios that require multiple contexts
    /// </summary>
    protected ApplicationDbContext CreateNewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Clears all data from the database
    /// </summary>
    protected void ClearDatabase()
    {
        Context.Users.RemoveRange(Context.Users);
        Context.Shipments.RemoveRange(Context.Shipments);
        Context.ShipmentItems.RemoveRange(Context.ShipmentItems);
        Context.SaveChanges();
    }

    /// <summary>
    /// Detaches all tracked entities to ensure fresh queries
    /// </summary>
    protected void DetachAllEntities()
    {
        var entries = Context.ChangeTracker.Entries().ToList();
        foreach (var entry in entries)
        {
            entry.State = EntityState.Detached;
        }
    }

    /// <summary>
    /// Cleanup: Dispose database and context
    /// </summary>
    public void Dispose()
    {
        Context?.Database.EnsureDeleted();
        Context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
