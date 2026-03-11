using LedgerCore.Core.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class NumberSeriesConfiguration : IEntityTypeConfiguration<NumberSeries>
{
    public void Configure(EntityTypeBuilder<NumberSeries> builder)
    {
        builder.ToTable("NumberSeries", t =>
        {
            t.HasCheckConstraint("CK_NumberSeries_Padding", "`Padding` > 0");
            t.HasCheckConstraint("CK_NumberSeries_CurrentNumber", "`CurrentNumber` >= 0");
        });

        builder.Property(x => x.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Prefix)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Suffix)
            .HasMaxLength(50);

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.HasIndex(x => new { x.EntityType, x.BranchId });
    }
}