using BlockchainAidTracker.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockchainAidTracker.DataAccess.Configurations;

/// <summary>
/// Entity Framework Core configuration for the ShipmentItem entity
/// </summary>
public class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
{
    public void Configure(EntityTypeBuilder<ShipmentItem> builder)
    {
        // Table name
        builder.ToTable("ShipmentItems");

        // Primary key
        builder.HasKey(si => si.Id);

        // Properties
        builder.Property(si => si.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(si => si.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(si => si.Quantity)
            .IsRequired();

        builder.Property(si => si.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(si => si.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(si => si.EstimatedValue)
            .HasColumnType("decimal(18,2)");

        // Shadow property for foreign key
        builder.Property<string>("ShipmentId")
            .IsRequired()
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(si => si.Category);
        builder.HasIndex("ShipmentId");
    }
}
