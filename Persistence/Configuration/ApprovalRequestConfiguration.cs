using LedgerCore.Core.Models.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("ApprovalRequests");

        builder.Property(x => x.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}