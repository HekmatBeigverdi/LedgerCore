using System.Linq.Expressions;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class InvoiceRepository(LedgerCoreDbContext context) : IInvoiceRepository
{
    public Task<SalesInvoice?> GetSalesInvoiceWithLinesAsync(int id, CancellationToken cancellationToken = default)
    {
        return context.SalesInvoices
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<PurchaseInvoice?> GetPurchaseInvoiceWithLinesAsync(int id, CancellationToken cancellationToken = default)
    {
        return context.PurchaseInvoices
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddSalesInvoiceAsync(SalesInvoice invoice, CancellationToken cancellationToken = default)
    {
        await context.SalesInvoices.AddAsync(invoice, cancellationToken);
    }

    public async Task AddPurchaseInvoiceAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default)
    {
        await context.PurchaseInvoices.AddAsync(invoice, cancellationToken);
    }

    public void UpdateSalesInvoice(SalesInvoice invoice)
    {
        context.SalesInvoices.Update(invoice);
    }

    public void UpdatePurchaseInvoice(PurchaseInvoice invoice)
    {
        context.PurchaseInvoices.Update(invoice);
    }

    public async Task<PagedResult<SalesInvoice>> QuerySalesAsync(PagingParams? paging = null,
        Expression<Func<SalesInvoice, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<SalesInvoice> query = context.SalesInvoices
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }

    public async Task<PagedResult<PurchaseInvoice>> QueryPurchaseAsync(PagingParams? paging = null,
        Expression<Func<PurchaseInvoice, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PurchaseInvoice> query = context.PurchaseInvoices
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}