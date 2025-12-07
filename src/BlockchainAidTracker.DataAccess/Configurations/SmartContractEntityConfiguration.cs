using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockchainAidTracker.DataAccess.Configurations;

/// <summary>
/// Entity Framework configuration for SmartContractEntity
/// </summary>
public class SmartContractEntityConfiguration : IEntityTypeConfiguration<SmartContractEntity>
{
    public void Configure(EntityTypeBuilder<SmartContractEntity> builder)
    {
        builder.ToTable("SmartContracts");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Id)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sc => sc.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sc => sc.Description)
            .HasMaxLength(1000);

        builder.Property(sc => sc.Version)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sc => sc.Type)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sc => sc.DeployedAt)
            .IsRequired();

        builder.Property(sc => sc.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(sc => sc.StateJson)
            .IsRequired()
            .HasDefaultValue("{}");

        builder.Property(sc => sc.LastUpdatedAt)
            .IsRequired();

        // Index for faster lookups
        builder.HasIndex(sc => sc.IsEnabled);
        builder.HasIndex(sc => sc.DeployedAt);
    }
}