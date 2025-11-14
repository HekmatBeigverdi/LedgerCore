namespace LedgerCore.Core.Models.Enums;

public enum ChequeStatus
{
    Issued = 1,        // صادر شده
    Received = 2,      // دریافت شده
    Delivered = 3,     // تحویل به شخص دیگر / بانک
    Returned = 4,      // برگشت خورده
    Cleared = 5,       // پاس شده
    Cancelled = 6      // ابطال شده
}