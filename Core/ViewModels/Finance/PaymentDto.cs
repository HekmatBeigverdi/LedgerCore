using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Finance;

public class PaymentDto
{
    public int Id { get; set; }
    public string Number { get; set; } = default!;
    public DateTime Date { get; set; }

    public int? PartyId { get; set; }          // معمولا تأمین‌کننده / بستانکار
    public string? PartyCode { get; set; }
    public string? PartyName { get; set; }

    public int? BranchId { get; set; }
    public string? BranchName { get; set; }

    public decimal Amount { get; set; }

    public int? CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal FxRate { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public int? BankAccountId { get; set; }
    public string? BankAccountTitle { get; set; }

    public string? CashDeskCode { get; set; }

    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }

    public DocumentStatus Status { get; set; }

    public int? JournalVoucherId { get; set; }
}