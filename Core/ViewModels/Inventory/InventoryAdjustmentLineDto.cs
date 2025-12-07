namespace LedgerCore.Core.ViewModels.Inventory;

public class InventoryAdjustmentLineDto
{
    public int ProductId { get; set; }

    /// <summary>
    /// مقدار تعدیل. مثبت یعنی افزایش موجودی، منفی یعنی کاهش موجودی.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// قیمت واحد پیشنهادی برای تعدیل. اگر ندهی، سیستم از AverageCost استفاده می‌کند.
    /// </summary>
    public decimal? UnitCost { get; set; }

    public string? Description { get; set; }
}