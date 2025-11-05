using LedgerCore.Core.Models.Accounting;

namespace LedgerCore.Core.Interfaces.Services;

public interface IAccountingService
{
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
}