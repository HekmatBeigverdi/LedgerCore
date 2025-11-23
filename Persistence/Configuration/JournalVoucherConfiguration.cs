using LedgerCore.Core.Models.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class JournalVoucherConfiguration : IEntityTypeConfiguration<JournalVoucher>
{
    public void Configure(EntityTypeBuilder<JournalVoucher> builder)
    {
        builder.ToTable("JournalVouchers");

        builder.Property(x => x.Number)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => x.Number).IsUnique();

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.JournalVoucher!)
            .HasForeignKey(x => x.JournalVoucherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
