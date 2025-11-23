using LedgerCore.Core.Models.Master;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class PartyCategoryConfiguration : IEntityTypeConfiguration<PartyCategory>
{
    public void Configure(EntityTypeBuilder<PartyCategory> builder)
    {
        builder.ToTable("PartyCategories");

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();
    }
}