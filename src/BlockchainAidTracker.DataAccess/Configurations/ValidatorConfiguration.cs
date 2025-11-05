using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockchainAidTracker.DataAccess.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Validator entity
/// </summary>
public class ValidatorConfiguration : IEntityTypeConfiguration<Validator>
{
    public void Configure(EntityTypeBuilder<Validator> builder)
    {
        // Table name
        builder.ToTable("Validators");

        // Primary key
        builder.HasKey(v => v.Id);

        // Properties
        builder.Property(v => v.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.PublicKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(v => v.EncryptedPrivateKey)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(v => v.Address)
            .HasMaxLength(200);

        builder.Property(v => v.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(v => v.Priority)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(v => v.CreatedTimestamp)
            .IsRequired();

        builder.Property(v => v.UpdatedTimestamp)
            .IsRequired();

        builder.Property(v => v.LastBlockCreatedTimestamp);

        builder.Property(v => v.TotalBlocksCreated)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(v => v.Description)
            .HasMaxLength(500);

        // Unique constraints
        builder.HasIndex(v => v.Name)
            .IsUnique();

        builder.HasIndex(v => v.PublicKey)
            .IsUnique();

        // Additional indexes for querying
        builder.HasIndex(v => v.IsActive);
        builder.HasIndex(v => v.Priority);
        builder.HasIndex(v => new { v.IsActive, v.Priority }); // Composite index for active validator selection
    }
}
