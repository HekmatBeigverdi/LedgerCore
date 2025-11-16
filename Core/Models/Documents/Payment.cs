using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Documents;

public class Payment: AuditableEntity
{
    public string Number { get; set; } = default!;
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int? PartyId { get; set; }               // معمولاً تأمین‌کننده
    public Party? Party { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public decimal Amount { get; set; }

    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public decimal FxRate { get; set; } = 1m;

    public int? BankAccountId { get; set; }         // از کدام حساب پرداخت شده
    public BankAccount? BankAccount { get; set; }

    public string? CashDeskCode { get; set; }       // اگر از صندوق نقدی

    public string? ReferenceNo { get; set; }        // شماره حواله/شماره چک و ...
    public string? Description { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public int? JournalVoucherId { get; set; }
    public JournalVoucher? JournalVoucher { get; set; }
}