using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Finance;

public class CreatePaymentRequest
{
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int? PartyId { get; set; }
    public int? BranchId { get; set; }

    public decimal Amount { get; set; }

    public int? CurrencyId { get; set; }
    public decimal FxRate { get; set; } = 1m;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public int? BankAccountId { get; set; }
    public string? CashDeskCode { get; set; }

    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
}

public class UpdatePaymentRequest : CreatePaymentRequest
{
}