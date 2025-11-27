using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Documents;

public class ReceiptDto
{
    public int Id { get; set; }

    // خود سند
    public string Number { get; set; } = default!;
    public DateTime Date { get; set; }

    // طرف حساب
    public int? PartyId { get; set; }
    public string? PartyCode { get; set; }
    public string? PartyName { get; set; }

    // شعبه
    public int? BranchId { get; set; }
    public string? BranchCode { get; set; }
    public string? BranchName { get; set; }

    // روش دریافت
    public PaymentMethod Method { get; set; }
    public string? MethodName { get; set; }

    // مبلغ و ارز
    public decimal Amount { get; set; }
    public int? CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public string? CurrencyName { get; set; }
    public decimal FxRate { get; set; }

    // حساب بانکی
    public int? BankAccountId { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountTitle { get; set; }
    public int? BankId { get; set; }
    public string? BankName { get; set; }

    // صندوق نقدی / توضیحات
    public string? CashDeskCode { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }

    // وضعیت سند و سند حسابداری
    public DocumentStatus Status { get; set; }
    public string? StatusName { get; set; }

    public int? JournalVoucherId { get; set; }
    public string? JournalVoucherNumber { get; set; }
}