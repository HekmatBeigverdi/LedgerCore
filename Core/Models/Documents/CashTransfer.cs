using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Documents;

public class CashTransfer: AuditableEntity
{
    public string Number { get; set; } = default!;
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int? FromBankAccountId { get; set; }
    public BankAccount? FromBankAccount { get; set; }

    public int? ToBankAccountId { get; set; }
    public BankAccount? ToBankAccount { get; set; }

    public string? FromCashDeskCode { get; set; }   // اگر از صندوق
    public string? ToCashDeskCode { get; set; }

    public decimal Amount { get; set; }

    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public decimal FxRate { get; set; } = 1m;

    public string? Description { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public int? JournalVoucherId { get; set; }
    public JournalVoucher? JournalVoucher { get; set; }
}