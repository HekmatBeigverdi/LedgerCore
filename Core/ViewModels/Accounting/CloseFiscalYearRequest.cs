namespace LedgerCore.Core.ViewModels.Accounting;

public class CloseFiscalYearRequest
{
    public int FiscalYearId { get; set; }

    // حساب سود و زیان (یا حساب انباشته سود و زیان/حقوق صاحبان سهام) که بستن درآمد/هزینه به آن منتقل می‌شود
    public int ProfitAndLossAccountId { get; set; }

    public bool CreateOpeningForNextYear { get; set; } = true;
}