using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.ViewModels.Documents;

namespace LedgerCore.Core.Interfaces.Services;

public interface ICashTransferService
{
    Task<CashTransfer> CreateCashTransferAsync(
        CashTransfer transfer,
        CancellationToken cancellationToken = default);

    Task<CashTransfer?> GetCashTransferAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task PostCashTransferAsync(
        int transferId,
        CancellationToken cancellationToken = default);
}