namespace LedgerCore.Core.Models.Enums;

public enum AssetTransactionType
{
    Acquisition = 1,    // تحصیل / خرید
    Improvement = 2,    // بهبود / افزایش ارزش
    Revaluation = 3,    // تجدید ارزیابی
    Depreciation = 4,   // استهلاک
    Disposal = 5,       // فروش / اسقاط
    Transfer = 6        // انتقال بین شعب/مراکز هزینه
}