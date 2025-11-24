using LedgerCore.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class ChequeConfiguration : IEntityTypeConfiguration<Cheque>
{
    public void Configure(EntityTypeBuilder<Cheque> builder)
    {
        builder.ToTable("Cheques");

        builder.Property(x => x.ChequeNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.ChequeNumber);
        builder.HasIndex(x => x.DueDate);
    }
}