namespace LedgerCore.Core.Models.Enums;

public enum AccountType
{
    Asset = 1,        // دارایی
    Liability = 2,    // بدهی
    Equity = 3,       // حقوق صاحبان سهام
    Revenue = 4,      // درآمد
    Expense = 5,      // هزینه
    OffBalance = 6    // حساب‌های انتظامی / تعهدی
}