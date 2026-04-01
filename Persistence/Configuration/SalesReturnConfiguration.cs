using LedgerCore.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("SalesReturns");

        builder.Property(x => x.Number)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Number).IsUnique();
        builder.HasIndex(x => x.Date);

        builder.Property(x => x.BranchId)
            .IsRequired();

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.SalesReturn!)
            .HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.JournalVoucher)
            .WithMany()
            .HasForeignKey(x => x.JournalVoucherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}