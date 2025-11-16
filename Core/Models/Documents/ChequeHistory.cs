using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Models.Documents;

public class ChequeHistory: BaseEntity
{
    public int ChequeId { get; set; }
    public Cheque? Cheque { get; set; }

    public DateTime ChangeDate { get; set; } = DateTime.UtcNow;
    public ChequeStatus Status { get; set; }

    public string? Description { get; set; }        // توضیح (مثلاً "تحویل بانک ملت شعبه X")
    public string? ChangedBy { get; set; }          // نام کاربر ثبت‌کننده
}