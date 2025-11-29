using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockchainAidTracker.DataAccess.Configurations;

/// <summary>
/// Entity Framework Core configuration for the DeliveryEvent entity
/// </summary>
public class DeliveryEventConfiguration : IEntityTypeConfiguration<DeliveryEvent>
{
    public void Configure(EntityTypeBuilder<DeliveryEvent> builder)
    {
        // Table name
        builder.ToTable("DeliveryEvents");

        // Primary key
        builder.HasKey(de => de.Id);

        // Properties
        builder.Property(de => de.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(de => de.ShipmentId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(de => de.EventType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(de => de.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(de => de.CreatedTimestamp)
            .IsRequired();

        builder.Property(de => de.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(de => de.Metadata)
            .HasMaxLength(4000);

        // Foreign keys
        builder.HasOne<Shipment>()
            .WithMany()
            .HasForeignKey(de => de.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(de => de.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(de => new { de.ShipmentId, de.CreatedTimestamp })
            .HasDatabaseName("IX_DeliveryEvents_ShipmentId_CreatedTimestamp");

        builder.HasIndex(de => de.EventType);
        builder.HasIndex(de => de.CreatedTimestamp);
    }
}
