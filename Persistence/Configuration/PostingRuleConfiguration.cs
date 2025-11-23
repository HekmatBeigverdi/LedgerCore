using LedgerCore.Core.Models.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class PostingRuleConfiguration : IEntityTypeConfiguration<PostingRule>
{
    public void Configure(EntityTypeBuilder<PostingRule> builder)
    {
        builder.ToTable("PostingRules");

        builder.Property(x => x.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => new { x.EntityType, x.Code }).IsUnique();
    }
}