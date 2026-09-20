using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Persistance.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public const string ActiveIdentityIndexName = "IX_Products_ActiveIdentity";

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Brand).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Category).IsRequired().HasMaxLength(100);
        builder.Property(p => p.UnitType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.UnitValue).HasPrecision(18, 3);
        builder.Property(p => p.Cost).HasPrecision(18, 2);
        builder.Property(p => p.SalePrice).HasPrecision(18, 2);
        builder.Property(p => p.IsActive);
        builder.Property(p => p.CreatedAt);
        builder.Property(p => p.UpdatedAt);

        builder
            .HasIndex(p => new { p.Name, p.Brand, p.Category, p.UnitType, p.UnitValue })
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName(ActiveIdentityIndexName);
    }
}
