using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockchainAidTracker.DataAccess.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Supplier entity
/// </summary>
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        // Table name
        builder.ToTable("Suppliers");

        // Primary key
        builder.HasKey(s => s.Id);

        // Properties
        builder.Property(s => s.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.RegistrationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.ContactEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.ContactPhone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.BusinessCategory)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.EncryptedBankDetails)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(s => s.PaymentThreshold)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(s => s.TaxId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.VerificationStatus)
            .IsRequired()
            .HasConversion<string>() // Store enum as string
            .HasMaxLength(20);

        builder.Property(s => s.CreatedTimestamp)
            .IsRequired();

        builder.Property(s => s.UpdatedTimestamp)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Foreign keys and relationships
        builder.HasOne(s => s.User)
            .WithMany(u => u.Suppliers)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.SupplierShipments)
            .WithOne(ss => ss.Supplier)
            .HasForeignKey(ss => ss.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.PaymentRecords)
            .WithOne(p => p.Supplier)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraints
        builder.HasIndex(s => s.CompanyName)
            .IsUnique();

        builder.HasIndex(s => s.RegistrationId)
            .IsUnique();

        builder.HasIndex(s => s.TaxId)
            .IsUnique();

        // Additional indexes for queries
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.VerificationStatus);
        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => new { s.IsActive, s.VerificationStatus });
    }
}
