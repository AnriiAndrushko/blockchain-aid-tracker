using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockchainAidTracker.DataAccess.Configurations;

/// <summary>
/// Entity Framework Core configuration for the PaymentRecord entity
/// </summary>
public class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        // Table name
        builder.ToTable("PaymentRecords");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.SupplierId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.ShipmentId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(p => p.PaymentMethod)
            .IsRequired()
            .HasConversion<string>() // Store enum as string
            .HasMaxLength(20);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>() // Store enum as string
            .HasMaxLength(20);

        builder.Property(p => p.BlockchainTransactionHash)
            .HasMaxLength(500);

        builder.Property(p => p.CreatedTimestamp)
            .IsRequired();

        builder.Property(p => p.CompletedTimestamp);

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);

        builder.Property(p => p.AttemptCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(p => p.ExternalPaymentReference)
            .HasMaxLength(500);

        // Foreign keys and relationships
        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.PaymentRecords)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Shipment)
            .WithMany()
            .HasForeignKey(p => p.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for common queries
        builder.HasIndex(p => p.SupplierId);
        builder.HasIndex(p => p.ShipmentId);
        builder.HasIndex(p => new { p.SupplierId, p.Status });
        builder.HasIndex(p => new { p.Status, p.CreatedTimestamp });
        builder.HasIndex(p => p.CreatedTimestamp);
        builder.HasIndex(p => new { p.SupplierId, p.CreatedTimestamp });
    }
}
