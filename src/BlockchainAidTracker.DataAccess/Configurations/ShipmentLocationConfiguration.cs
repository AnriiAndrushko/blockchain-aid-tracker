using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockchainAidTracker.DataAccess.Configurations;

/// <summary>
/// Entity Framework Core configuration for the ShipmentLocation entity
/// </summary>
public class ShipmentLocationConfiguration : IEntityTypeConfiguration<ShipmentLocation>
{
    public void Configure(EntityTypeBuilder<ShipmentLocation> builder)
    {
        // Table name
        builder.ToTable("ShipmentLocations");

        // Primary key
        builder.HasKey(sl => sl.Id);

        // Properties
        builder.Property(sl => sl.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sl => sl.ShipmentId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sl => sl.Latitude)
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(sl => sl.Longitude)
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(sl => sl.LocationName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(sl => sl.CreatedTimestamp)
            .IsRequired();

        builder.Property(sl => sl.GpsAccuracy)
            .HasColumnType("decimal(18,2)");

        builder.Property(sl => sl.UpdatedByUserId)
            .IsRequired()
            .HasMaxLength(50);

        // Foreign keys
        builder.HasOne<Shipment>()
            .WithMany()
            .HasForeignKey(sl => sl.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(sl => sl.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(sl => new { sl.ShipmentId, sl.CreatedTimestamp })
            .HasDatabaseName("IX_ShipmentLocations_ShipmentId_CreatedTimestamp");

        builder.HasIndex(sl => sl.CreatedTimestamp);
    }
}
