using System.Linq.Expressions;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;

namespace LedgerCore.Core.Interfaces.Repositories;

/// <summary>
/// Invoice repository covers both Sales & Purchase invoices.
/// </summary>
public interface IInvoiceRepository
{
    Task<SalesInvoice?> GetSalesInvoiceWithLinesAsync(int id, int branchId, CancellationToken cancellationToken = default);
    Task<PurchaseInvoice?> GetPurchaseInvoiceWithLinesAsync(int id, int branchId, CancellationToken cancellationToken = default);

    Task AddSalesInvoiceAsync(SalesInvoice invoice, CancellationToken cancellationToken = default);
    Task AddPurchaseInvoiceAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default);

    void UpdateSalesInvoice(SalesInvoice invoice);
    void UpdatePurchaseInvoice(PurchaseInvoice invoice);

    Task<PagedResult<SalesInvoice>> QuerySalesAsync(int branchId, PagingParams? paging = null,
        Expression<Func<SalesInvoice, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PurchaseInvoice>> QueryPurchaseAsync(int branchId, PagingParams? paging = null,
        Expression<Func<PurchaseInvoice, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
    
    Task<SalesReturn?> GetSalesReturnWithLinesAsync(int id, CancellationToken cancellationToken = default);
    Task<PurchaseReturn?> GetPurchaseReturnWithLinesAsync(int id, CancellationToken cancellationToken = default);

    Task AddSalesReturnAsync(SalesReturn document, CancellationToken cancellationToken = default);
    Task AddPurchaseReturnAsync(PurchaseReturn document, CancellationToken cancellationToken = default);

    void UpdateSalesReturn(SalesReturn document);
    void UpdatePurchaseReturn(PurchaseReturn document);

    Task<PagedResult<SalesReturn>> QuerySalesReturnsAsync(
        PagingParams? paging = null,
        Expression<Func<SalesReturn, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PurchaseReturn>> QueryPurchaseReturnsAsync(
        PagingParams? paging = null,
        Expression<Func<PurchaseReturn, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}