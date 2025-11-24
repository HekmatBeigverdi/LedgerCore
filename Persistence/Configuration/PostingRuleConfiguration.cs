using LedgerCore.Core.Models.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class PostingRuleConfiguration : IEntityTypeConfiguration<PostingRule>
{
    public void Configure(EntityTypeBuilder<PostingRule> builder)
    {
        builder.ToTable("PostingRules");

        builder.Property(x => x.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DocumentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        // روابط
        builder.HasOne(x => x.DebitAccount)
            .WithMany()
            .HasForeignKey(x => x.DebitAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreditAccount)
            .WithMany()
            .HasForeignKey(x => x.CreditAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TaxAccount)
            .WithMany()
            .HasForeignKey(x => x.TaxAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DiscountAccount)
            .WithMany()
            .HasForeignKey(x => x.DiscountAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DocumentType, x.Code }).IsUnique();
        builder.HasIndex(x => x.DocumentType);
    }
}