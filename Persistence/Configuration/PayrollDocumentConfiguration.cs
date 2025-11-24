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

        builder.HasIndex(x => x.Number).IsUnique();

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.PayrollDocument!)
            .HasForeignKey(x => x.PayrollDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}