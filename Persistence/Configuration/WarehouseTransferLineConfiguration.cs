using LedgerCore.Core.Models.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class WarehouseTransferLineConfiguration : IEntityTypeConfiguration<WarehouseTransferLine>
{
    public void Configure(EntityTypeBuilder<WarehouseTransferLine> builder)
    {
        builder.ToTable("WarehouseTransferLines");

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 4);

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 4);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.WarehouseTransferId, x.LineNumber })
            .IsUnique();
    }
}