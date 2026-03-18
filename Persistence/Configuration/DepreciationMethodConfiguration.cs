using LedgerCore.Core.Models.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class DepreciationMethodConfiguration : IEntityTypeConfiguration<DepreciationMethod>
{
    public void Configure(EntityTypeBuilder<DepreciationMethod> builder)
    {
        builder.ToTable("DepreciationMethods");

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
