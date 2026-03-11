using LedgerCore.Core.Models.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class PayrollDocumentConfiguration : IEntityTypeConfiguration<PayrollDocument>
{
    public void Configure(EntityTypeBuilder<PayrollDocument> builder)
    {
        builder.ToTable("PayrollDocuments");

        builder.Property(x => x.Number)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.BranchId)
            .IsRequired();

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.Number }).IsUnique();
        builder.HasIndex(x => x.Date);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.PayrollDocument!)
            .HasForeignKey(x => x.PayrollDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}