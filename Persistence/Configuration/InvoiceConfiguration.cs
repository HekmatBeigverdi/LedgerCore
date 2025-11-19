using LedgerCore.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("SalesInvoices");

        builder.Property(x => x.Number)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Number)
            .IsUnique();

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.SalesInvoice!)
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
public class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.ToTable("PurchaseInvoices");

        builder.Property(x => x.Number)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Number)
            .IsUnique();

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.PurchaseInvoice!)
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}