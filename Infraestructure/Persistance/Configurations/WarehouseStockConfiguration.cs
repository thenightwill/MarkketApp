using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Persistance.Configurations;

public class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public const string BatchIndexName = "IX_WarehouseStocks_ProductId_BatchNumber";

    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.ToTable("WarehouseStocks", t => t.HasCheckConstraint("CK_WarehouseStocks_Quantity", "[Quantity] >= 0"));
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.BatchNumber).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Location).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Quantity).HasPrecision(18, 3);
        builder.Property(s => s.MinimumStock).HasPrecision(18, 3);
        builder.Property(s => s.ReceivedDate);
        builder.Property(s => s.ExpirationDate);
        builder.Property(s => s.IsActive);
        builder.Property(s => s.CreatedAt);
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasOne<Product>().WithMany().HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.ProductId, s.BatchNumber }).IsUnique().HasDatabaseName(BatchIndexName);
        builder.HasIndex(s => new { s.ProductId, s.ExpirationDate });
    }
}
