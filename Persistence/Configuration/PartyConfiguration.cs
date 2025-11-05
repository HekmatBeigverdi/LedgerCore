using LedgerCore.Core.Models.Master;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class PartyConfiguration: IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        builder.ToTable("Parties");

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DefaultCurrency)
            .WithMany()
            .HasForeignKey(x => x.DefaultCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}