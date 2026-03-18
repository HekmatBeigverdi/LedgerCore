using LedgerCore.Core.Models.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems");

        builder.HasIndex(x => new { x.WarehouseId, x.ProductId }).IsUnique();
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.WarehouseId);
    }
}