namespace LedgerCore.Core.Models.Enums;

public enum DocumentStatus
{
    Draft = 0,      // پیش‌نویس
    Pending = 1,    // در انتظار تأیید
    Approved = 2,   // تأیید شده
    Posted = 3,     // ارسال شده به دفتر (حسابداری)
    Cancelled = 4   // ابطال شده
}