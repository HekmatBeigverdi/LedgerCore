using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Models.Workflow;

public class ApprovalStep: BaseEntity
{
    public int ApprovalRequestId { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; }

    public int StepOrder { get; set; }                 // ترتیب مرحله (1,2,3,...)

    public string RoleName { get; set; } = default!;   // نقشی که باید تأیید کند (مثلا "FinanceManager")
    public string? UserName { get; set; }              // در صورت تخصیص به کاربر خاص

    public bool IsRequired { get; set; } = true;       // آیا مرحله اجباری است؟

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ActionAt { get; set; }
    public string? ActionBy { get; set; }
    public string? Comment { get; set; }
}