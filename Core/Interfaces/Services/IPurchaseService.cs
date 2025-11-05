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
}