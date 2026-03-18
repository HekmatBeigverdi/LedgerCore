using LedgerCore.Core.Models.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class TrialBalanceRowConfiguration : IEntityTypeConfiguration<TrialBalanceRow>
{
    public void Configure(EntityTypeBuilder<TrialBalanceRow> builder)
    {
        builder.ToTable("TrialBalanceRows");
    }
}