using LedgerCore.Core.Models.Documents;

namespace LedgerCore.Core.Interfaces.Services;

public interface IPurchaseService
{
    Task<PurchaseInvoice> CreatePurchaseInvoiceAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoice?> GetPurchaseInvoiceAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoice> UpdatePurchaseInvoiceAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default);

    Task PostPurchaseInvoiceAsync(
        int invoiceId,
        CancellationToken cancellationToken = default);
    Task<PurchaseReturn> CreatePurchaseReturnAsync(
        PurchaseReturn document,
        CancellationToken cancellationToken = default);

    Task<PurchaseReturn?> GetPurchaseReturnAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PurchaseReturn> UpdatePurchaseReturnAsync(
        PurchaseReturn document,
        CancellationToken cancellationToken = default);

    Task PostPurchaseReturnAsync(
        int documentId,
        CancellationToken cancellationToken = default);
}