using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockchainAidTracker.DataAccess.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Shipment entity
/// </summary>
public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        // Table name
        builder.ToTable("Shipments");

        // Primary key
        builder.HasKey(s => s.Id);

        // Properties
        builder.Property(s => s.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Origin)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Destination)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.ExpectedDeliveryTimeframe)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.AssignedRecipient)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>() // Store enum as string
            .HasMaxLength(20);

        builder.Property(s => s.QrCodeData)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.CreatedTimestamp)
            .IsRequired();

        builder.Property(s => s.UpdatedTimestamp)
            .IsRequired();

        builder.Property(s => s.CoordinatorPublicKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.DonorPublicKey)
            .HasMaxLength(500);

        builder.Property(s => s.Notes)
            .HasMaxLength(2000);

        builder.Property(s => s.TotalEstimatedValue)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // Relationships
        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey("ShipmentId")
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.CreatedTimestamp);
        builder.HasIndex(s => s.AssignedRecipient);
        builder.HasIndex(s => s.CoordinatorPublicKey);
        builder.HasIndex(s => s.QrCodeData)
            .IsUnique();
    }
}
