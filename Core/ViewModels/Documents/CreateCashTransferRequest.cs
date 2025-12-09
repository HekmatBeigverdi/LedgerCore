namespace LedgerCore.Core.ViewModels.Documents;

public class CreateCashTransferRequest
{
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int? FromBankAccountId { get; set; }
    public string? FromCashDeskCode { get; set; }

    public int? ToBankAccountId { get; set; }
    public string? ToCashDeskCode { get; set; }

    public decimal Amount { get; set; }

    public int? CurrencyId { get; set; }
    public decimal FxRate { get; set; } = 1m;

    public string? Description { get; set; }
}

public class UpdateCashTransferRequest : CreateCashTransferRequest
{
    // فعلاً چیز اضافه‌ای نداریم؛ اگر بعداً خواستی Status یا چیز دیگری قابل ویرایش باشد، اینجا اضافه کن
}