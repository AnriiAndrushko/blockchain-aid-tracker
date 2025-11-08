using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockchainAidTracker.DataAccess.Configurations;

/// <summary>
/// Entity Framework Core configuration for the AuditLog entity
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Table name
        builder.ToTable("AuditLogs");

        // Primary key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Category)
            .IsRequired()
            .HasConversion<string>() // Store enum as string
            .HasMaxLength(30);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasConversion<string>() // Store enum as string
            .HasMaxLength(50);

        builder.Property(a => a.UserId)
            .HasMaxLength(50);

        builder.Property(a => a.Username)
            .HasMaxLength(50);

        builder.Property(a => a.EntityId)
            .HasMaxLength(50);

        builder.Property(a => a.EntityType)
            .HasMaxLength(50);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Metadata)
            .HasMaxLength(2000); // JSON metadata

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        builder.Property(a => a.IsSuccess)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        // Indexes for efficient querying
        builder.HasIndex(a => a.Category);
        builder.HasIndex(a => a.Action);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.EntityId);
        builder.HasIndex(a => a.EntityType);
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.IsSuccess);

        // Composite indexes for common query patterns
        builder.HasIndex(a => new { a.Category, a.Timestamp });
        builder.HasIndex(a => new { a.UserId, a.Timestamp });
        builder.HasIndex(a => new { a.EntityId, a.Timestamp });
    }
}
