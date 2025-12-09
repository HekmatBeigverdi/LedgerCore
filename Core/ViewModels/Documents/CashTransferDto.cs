using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Documents;

public class CashTransferDto
{
    public int Id { get; set; }

    public string Number { get; set; } = default!;
    public DateTime Date { get; set; }

    public int? FromBankAccountId { get; set; }
    public string? FromBankAccountTitle { get; set; }
    public string? FromCashDeskCode { get; set; }

    public int? ToBankAccountId { get; set; }
    public string? ToBankAccountTitle { get; set; }
    public string? ToCashDeskCode { get; set; }

    public decimal Amount { get; set; }

    public int? CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal FxRate { get; set; }

    public string? Description { get; set; }

    public DocumentStatus Status { get; set; }
    public string? StatusName { get; set; }

    public int? JournalVoucherId { get; set; }
    public string? JournalVoucherNumber { get; set; }
}