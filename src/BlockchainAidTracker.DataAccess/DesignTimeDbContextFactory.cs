using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BlockchainAidTracker.DataAccess;

/// <summary>
/// Design-time factory for creating ApplicationDbContext instances for EF Core migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use SQLite for migrations
        optionsBuilder.UseSqlite("Data Source=blockchain-aid-tracker.db");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
