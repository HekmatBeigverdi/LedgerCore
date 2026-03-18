using LedgerCore.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class ChequeHistoryConfiguration : IEntityTypeConfiguration<ChequeHistory>
{
    public void Configure(EntityTypeBuilder<ChequeHistory> builder)
    {
        builder.ToTable("ChequeHistories");

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.ChangedBy)
            .HasMaxLength(100);

        builder.HasOne(x => x.Cheque)
            .WithMany(x => x.History)
            .HasForeignKey(x => x.ChequeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.JournalVoucher)
            .WithMany()
            .HasForeignKey(x => x.JournalVoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ChequeId);

        builder.HasIndex(x => new { x.ChequeId, x.ChangeDate });

        builder.HasIndex(x => x.JournalVoucherId);
    }
}