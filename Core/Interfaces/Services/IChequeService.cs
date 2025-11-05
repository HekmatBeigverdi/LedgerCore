using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Interfaces.Services;

public interface IChequeService
{
    Task<Cheque> RegisterChequeAsync(
        Cheque cheque,
        CancellationToken cancellationToken = default);

    Task ChangeStatusAsync(
        int chequeId,
        ChequeStatus newStatus,
        string? comment,
        CancellationToken cancellationToken = default);
}