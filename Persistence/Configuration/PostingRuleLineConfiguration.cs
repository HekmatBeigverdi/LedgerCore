using LedgerCore.Core.Models.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class PostingRuleLineConfiguration : IEntityTypeConfiguration<PostingRuleLine>
{
    public void Configure(EntityTypeBuilder<PostingRuleLine> builder)
    {
        builder.ToTable("PostingRuleLines");

        builder.Property(x => x.LineNumber)
            .IsRequired();

        builder.Property(x => x.FixedAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.DescriptionTemplate)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.UsePartyFromDocument)
            .HasDefaultValue(false);

        builder.HasOne(x => x.PostingRule)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.PostingRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PostingRuleId, x.LineNumber })
            .IsUnique();

        builder.HasIndex(x => x.AccountId);
        
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}