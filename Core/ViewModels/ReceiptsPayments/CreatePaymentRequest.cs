using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.ReceiptsPayments;

public class CreatePaymentRequest
{
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int? SupplierId { get; set; }

    public decimal Amount { get; set; }

    public int? CurrencyId { get; set; }
    public decimal FxRate { get; set; } = 1m;

    public PaymentMethod PaymentMethod { get; set; }

    public int? CashAccountId { get; set; }
    public int? BankAccountId { get; set; }
    public int? ChequeId { get; set; }

    public string? Description { get; set; }
}

public class UpdatePaymentRequest : CreatePaymentRequest
{
}