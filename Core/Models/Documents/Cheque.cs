using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Documents;

public class Cheque: AuditableEntity
{
    public string ChequeNumber { get; set; } = default!;      // شماره چک
    public string? Serial { get; set; }                       // سریال/سری چک

    public bool IsIncoming { get; set; }                      // true: چک دریافتی، false: صادره

    public int? PartyId { get; set; }                         // در چک دریافتی: صادرکننده، در صادره: ذی‌نفع
    public Party? Party { get; set; }

    public int? BankAccountId { get; set; }                   // حساب بانکی مرتبط (در صادره یا هنگام واگذاری)
    public BankAccount? BankAccount { get; set; }

    public decimal Amount { get; set; }

    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public decimal FxRate { get; set; } = 1m;

    public DateTime IssueDate { get; set; }                   // تاریخ صدور
    public DateTime DueDate { get; set; }                     // تاریخ سررسید

    public ChequeStatus Status { get; set; } = ChequeStatus.Issued;

    public string? Description { get; set; }

    public ICollection<ChequeHistory> History { get; set; } = new List<ChequeHistory>();
    
}