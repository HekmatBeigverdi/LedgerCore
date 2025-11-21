using System.Linq.Expressions;
using LedgerCore.Core.Models.Common;
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

    // Queries
    Task<PagedResult<SalesInvoice>> QuerySalesAsync(PagingParams? paging = null, 
        Expression<Func<SalesInvoice, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PurchaseInvoice>> QueryPurchaseAsync(PagingParams? paging = null, 
        Expression<Func<PurchaseInvoice, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}