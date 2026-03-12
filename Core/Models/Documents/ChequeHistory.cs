using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Models.Documents;

public class ChequeHistory : BaseEntity
{
    public int ChequeId { get; set; }
    public Cheque Cheque { get; set; } = default!;

    public int? JournalVoucherId { get; set; }
    public JournalVoucher? JournalVoucher { get; set; }

    public DateTime ChangeDate { get; set; }

    public ChequeStatus Status { get; set; }

    public string? Description { get; set; }
    public string? ChangedBy { get; set; }
}