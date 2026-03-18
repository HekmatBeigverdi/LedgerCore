using LedgerCore.Core.Models.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Persistence.Configuration;

public class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("ApprovalSteps");

        builder.Property(x => x.RoleName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => new { x.ApprovalRequestId, x.StepOrder }).IsUnique();
    }
}