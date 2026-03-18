using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Accounting;

public class JournalLine: BaseEntity
{
    public int JournalVoucherId { get; set; }
    public JournalVoucher? JournalVoucher { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    public int? PartyId { get; set; }
    public Party? Party { get; set; }

    public int? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public decimal FxRate { get; set; } = 1m;

    public string? Description { get; set; }

    public string? RefDocumentType { get; set; }    // Invoice, Receipt, Payment, ...
    public int? RefDocumentId { get; set; }         // شناسه سند مرجع

    public int LineNumber { get; set; }             // برای مرتب‌سازی سطور
}