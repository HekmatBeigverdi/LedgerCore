using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Models.Workflow;

public class ApprovalRequest: AuditableEntity
{
    /// <summary>
    /// نوع سند: "SalesInvoice", "PurchaseInvoice", "Journal", ...
    /// </summary>
    public string EntityType { get; set; } = default!;

    /// <summary>
    /// آیدی سند در جدول مربوطه
    /// </summary>
    public int EntityId { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public string? RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public string? LastActionBy { get; set; }
    public DateTime? LastActionAt { get; set; }

    public string? LastActionComment { get; set; }

    public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
}