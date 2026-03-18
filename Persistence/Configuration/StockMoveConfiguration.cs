using LedgerCore.Core.Models.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class StockMoveConfiguration : IEntityTypeConfiguration<StockMove>
{
    public void Configure(EntityTypeBuilder<StockMove> builder)
    {
        builder.ToTable("StockMoves");

        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.WarehouseId);
    }
}
