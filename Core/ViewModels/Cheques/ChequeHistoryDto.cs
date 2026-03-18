using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Cheques;

public class ChequeHistoryDto
{
    public int Id { get; set; }

    public int? JournalVoucherId { get; set; }
    public string? JournalVoucherNumber { get; set; }

    public DateTime ChangeDate { get; set; }

    public ChequeStatus Status { get; set; }
    public string StatusName { get; set; } = default!;

    public string? Description { get; set; }
    public string? ChangedBy { get; set; }
}