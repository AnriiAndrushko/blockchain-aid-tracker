using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BlockchainAidTracker.DataAccess;

/// <summary>
/// Entity Framework Core database context for the blockchain aid tracker application
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// DbSet for Shipment entities
    /// </summary>
    public DbSet<Shipment> Shipments { get; set; } = null!;

    /// <summary>
    /// DbSet for ShipmentItem entities
    /// </summary>
    public DbSet<ShipmentItem> ShipmentItems { get; set; } = null!;

    /// <summary>
    /// DbSet for User entities
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// DbSet for Validator entities
    /// </summary>
    public DbSet<Validator> Validators { get; set; } = null!;

    /// <summary>
    /// DbSet for Supplier entities
    /// </summary>
    public DbSet<Supplier> Suppliers { get; set; } = null!;

    /// <summary>
    /// DbSet for SupplierShipment entities
    /// </summary>
    public DbSet<SupplierShipment> SupplierShipments { get; set; } = null!;

    /// <summary>
    /// DbSet for PaymentRecord entities
    /// </summary>
    public DbSet<PaymentRecord> PaymentRecords { get; set; } = null!;

    /// <summary>
    /// Constructor for dependency injection
    /// </summary>
    /// <param name="options">DbContext options</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configures entity mappings and relationships
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new ShipmentConfiguration());
        modelBuilder.ApplyConfiguration(new ShipmentItemConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ValidatorConfiguration());
        modelBuilder.ApplyConfiguration(new SupplierConfiguration());
        modelBuilder.ApplyConfiguration(new SupplierShipmentConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentRecordConfiguration());
    }

    /// <summary>
    /// Overrides SaveChanges to automatically update timestamps
    /// </summary>
    /// <returns>Number of entities written to the database</returns>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Overrides SaveChangesAsync to automatically update timestamps
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities written to the database</returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates timestamps for modified entities
    /// </summary>
    private void UpdateTimestamps()
    {
        var shipmentEntries = ChangeTracker.Entries<Shipment>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in shipmentEntries)
        {
            entry.Entity.UpdatedTimestamp = DateTime.UtcNow;
        }

        var userEntries = ChangeTracker.Entries<User>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in userEntries)
        {
            entry.Entity.UpdatedTimestamp = DateTime.UtcNow;
        }

        var validatorEntries = ChangeTracker.Entries<Validator>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in validatorEntries)
        {
            entry.Entity.UpdatedTimestamp = DateTime.UtcNow;
        }

        var supplierEntries = ChangeTracker.Entries<Supplier>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in supplierEntries)
        {
            entry.Entity.UpdatedTimestamp = DateTime.UtcNow;
        }
    }
}
