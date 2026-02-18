using LedgerCore.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

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
        
        builder.Property(x => x.BranchId)
            .IsRequired();

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}