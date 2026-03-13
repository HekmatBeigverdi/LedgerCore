using LedgerCore.Core.Models.Accounting;

namespace LedgerCore.Core.Interfaces.Services;

public interface IPostingEngineService
{
    Task<JournalVoucher> BuildJournalAsync(
        string documentType,
        int branchId,
        DateTime date,
        int refDocumentId,
        string? refDocumentNumber,
        PostingContext context,
        CancellationToken cancellationToken = default);
}