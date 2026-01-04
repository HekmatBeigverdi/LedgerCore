using LedgerCore.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts");

        builder.Property(x => x.Number)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Number).IsUnique();
        builder.HasIndex(x => x.Date);
        
        builder.HasOne(x => x.JournalVoucher)
            .WithMany()
            .HasForeignKey(x => x.JournalVoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReversalJournalVoucher)
            .WithMany()
            .HasForeignKey(x => x.ReversalJournalVoucherId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}