using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;

namespace LedgerCore.Core.Interfaces.Services;

public interface IAccountingService
{
    // ==== متدهای سند حسابداری (Journal) ====
    Task<JournalVoucher> CreateJournalAsync(JournalVoucher voucher, CancellationToken cancellationToken = default);
    Task<JournalVoucher?> GetJournalAsync(int id, CancellationToken cancellationToken = default);
    Task<JournalVoucher> UpdateJournalAsync(JournalVoucher voucher, CancellationToken cancellationToken = default);
    Task DeleteJournalAsync(int id, CancellationToken cancellationToken = default);
    Task PostJournalAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsBalancedAsync(int journalId, CancellationToken cancellationToken = default);
    Task<JournalVoucher> ReverseJournalAsync(int journalId, DateTime? reversalDate = null, string? description = null, CancellationToken cancellationToken = default);
    
    // ==== Fiscal ====
    Task CloseFiscalPeriodAsync(int fiscalPeriodId, int profitAndLossAccountId,  CancellationToken cancellationToken = default);
    Task CloseFiscalYearAsync(int fiscalYearId, int profitAndLossAccountId, bool createOpeningForNextYear = true, CancellationToken cancellationToken = default);
    
    // ==== Receipt ====
    Task<Receipt> CreateReceiptAsync(Receipt receipt, CancellationToken cancellationToken = default);
    Task<Receipt?> GetReceiptAsync(int id, CancellationToken cancellationToken = default);
    Task<Receipt> UpdateReceiptAsync(Receipt receipt, CancellationToken cancellationToken = default);
    Task PostReceiptAsync(int receiptId, CancellationToken cancellationToken = default);

    // ==== Payment ====
    Task<Payment> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentAsync(int id, CancellationToken cancellationToken = default);
    Task<Payment> UpdatePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task PostPaymentAsync(int paymentId, CancellationToken cancellationToken = default);
    
    // ==== Inventory Adjustment Posting ====

    /// <summary>
    /// ثبت سند حسابداری برای تعدیل موجودی انبار بر اساس TotalDifferenceValue.
    /// فرض: موجودی انبار قبلاً در InventoryService محاسبه و TotalDifferenceValue تنظیم شده است.
    /// </summary>
    Task PostInventoryAdjustmentAsync(int inventoryAdjustmentId, CancellationToken cancellationToken = default);

}