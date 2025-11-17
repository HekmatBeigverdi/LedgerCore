using LedgerCore.Core.Models.Documents;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IInvoiceRepository
{
    Task<SalesInvoice?> GetSalesInvoiceWithLinesAsync(int id, CancellationToken cancellationToken = default);
    Task<PurchaseInvoice?> GetPurchaseInvoiceWithLinesAsync(int id, CancellationToken cancellationToken = default);

    Task AddSalesInvoiceAsync(SalesInvoice invoice, CancellationToken cancellationToken = default);
    Task AddPurchaseInvoiceAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default);

    void UpdateSalesInvoice(SalesInvoice invoice);
    void UpdatePurchaseInvoice(PurchaseInvoice invoice);
}