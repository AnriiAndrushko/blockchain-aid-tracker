using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockchainAidTracker.DataAccess.Configurations;

/// <summary>
/// Entity Framework Core configuration for the SupplierShipment entity
/// </summary>
public class SupplierShipmentConfiguration : IEntityTypeConfiguration<SupplierShipment>
{
    public void Configure(EntityTypeBuilder<SupplierShipment> builder)
    {
        // Table name
        builder.ToTable("SupplierShipments");

        // Primary key
        builder.HasKey(ss => ss.Id);

        // Properties
        builder.Property(ss => ss.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ss => ss.SupplierId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ss => ss.ShipmentId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ss => ss.GoodsDescription)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ss => ss.Quantity)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(ss => ss.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ss => ss.Value)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(ss => ss.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(ss => ss.ProvidedTimestamp)
            .IsRequired();

        builder.Property(ss => ss.PaymentReleased)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ss => ss.PaymentReleasedTimestamp);

        builder.Property(ss => ss.PaymentTransactionReference)
            .HasMaxLength(500);

        builder.Property(ss => ss.PaymentStatus)
            .IsRequired()
            .HasConversion<string>() // Store enum as string
            .HasMaxLength(20);

        // Foreign keys and relationships
        builder.HasOne(ss => ss.Supplier)
            .WithMany(s => s.SupplierShipments)
            .HasForeignKey(ss => ss.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ss => ss.Shipment)
            .WithMany()
            .HasForeignKey(ss => ss.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for common queries
        builder.HasIndex(ss => ss.SupplierId);
        builder.HasIndex(ss => ss.ShipmentId);
        builder.HasIndex(ss => new { ss.SupplierId, ss.ShipmentId });
        builder.HasIndex(ss => new { ss.SupplierId, ss.PaymentStatus });
        builder.HasIndex(ss => new { ss.PaymentReleased, ss.PaymentStatus });
    }
}
