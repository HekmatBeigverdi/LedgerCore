using LedgerCore.Core.Models.Documents;

namespace LedgerCore.Core.Interfaces.Services;

public interface ISalesService
{
    Task<SalesInvoice> CreateSalesInvoiceAsync(
        SalesInvoice invoice,
        CancellationToken cancellationToken = default);

    Task<SalesInvoice?> GetSalesInvoiceAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<SalesInvoice> UpdateSalesInvoiceAsync(
        SalesInvoice invoice,
        CancellationToken cancellationToken = default);

    Task PostSalesInvoiceAsync(
        int invoiceId,
        CancellationToken cancellationToken = default);
}
