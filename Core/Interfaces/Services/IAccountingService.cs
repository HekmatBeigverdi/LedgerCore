using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;

namespace LedgerCore.Core.Interfaces.Services;

public interface IAccountingService
{
    // ==== متدهای قبلی برای سند حسابداری ====
    Task<JournalVoucher> CreateJournalAsync(
        JournalVoucher voucher,
        CancellationToken cancellationToken = default);

    Task<JournalVoucher?> GetJournalAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task PostJournalAsync(
        int journalId,
        CancellationToken cancellationToken = default);

    Task CloseFiscalPeriodAsync(
        int fiscalPeriodId,
        CancellationToken cancellationToken = default);

    Task<bool> IsBalancedAsync(
        int journalId,
        CancellationToken cancellationToken = default);
    
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
}