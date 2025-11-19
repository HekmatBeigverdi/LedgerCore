using LedgerCore.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");

        builder.Property(x => x.LineNumber)
            .IsRequired();

        builder.HasIndex(x => new { x.SalesInvoiceId, x.LineNumber });
        builder.HasIndex(x => new { x.PurchaseInvoiceId, x.LineNumber });
    }
}