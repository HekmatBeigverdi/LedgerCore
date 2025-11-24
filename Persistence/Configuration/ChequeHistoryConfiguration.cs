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
    }
}