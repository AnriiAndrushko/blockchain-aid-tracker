using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BlockchainAidTracker.Api;

public partial class BlockchainAidTrackerContext : DbContext
{
    public BlockchainAidTrackerContext()
    {
    }

    public BlockchainAidTrackerContext(DbContextOptions<BlockchainAidTrackerContext> options)
        : base(options)
    {
    }

    public virtual DbSet<SmartContract> SmartContracts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SmartContract>(entity =>
        {
            entity.HasIndex(e => e.DeployedAt, "IX_SmartContracts_DeployedAt");

            entity.HasIndex(e => e.IsEnabled, "IX_SmartContracts_IsEnabled");

            entity.Property(e => e.IsEnabled).HasDefaultValue(1);
            entity.Property(e => e.StateJson).HasDefaultValue("{}");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
