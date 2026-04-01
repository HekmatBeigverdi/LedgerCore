using System.Linq.Expressions;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class InvoiceRepository(LedgerCoreDbContext context) : IInvoiceRepository
{
    
    public Task<SalesInvoice?> GetSalesInvoiceWithLinesAsync(int id, int branchId, CancellationToken cancellationToken = default)
    {
        return context.SalesInvoices
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId, cancellationToken);
    }

    public Task<PurchaseInvoice?> GetPurchaseInvoiceWithLinesAsync(int id, int branchId, CancellationToken cancellationToken = default)
    {
        return context.PurchaseInvoices
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId, cancellationToken);
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
    
    public async Task<PagedResult<SalesInvoice>> QuerySalesAsync(
        int branchId,
        PagingParams? paging = null,
        Expression<Func<SalesInvoice, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<SalesInvoice> query = context.SalesInvoices
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Where(x => x.BranchId == branchId)
            .AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }

    public async Task<PagedResult<PurchaseInvoice>> QueryPurchaseAsync(
        int branchId,
        PagingParams? paging = null,
        Expression<Func<PurchaseInvoice, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PurchaseInvoice> query = context.PurchaseInvoices
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Where(x => x.BranchId == branchId)
            .AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
    public Task<SalesReturn?> GetSalesReturnWithLinesAsync(int id, CancellationToken cancellationToken = default)
    {
        return context.SalesReturns
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<PurchaseReturn?> GetPurchaseReturnWithLinesAsync(int id, CancellationToken cancellationToken = default)
    {
        return context.PurchaseReturns
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddSalesReturnAsync(SalesReturn document, CancellationToken cancellationToken = default)
    {
        await context.SalesReturns.AddAsync(document, cancellationToken);
    }

    public async Task AddPurchaseReturnAsync(PurchaseReturn document, CancellationToken cancellationToken = default)
    {
        await context.PurchaseReturns.AddAsync(document, cancellationToken);
    }

    public void UpdateSalesReturn(SalesReturn document)
    {
        context.SalesReturns.Update(document);
    }

    public void UpdatePurchaseReturn(PurchaseReturn document)
    {
        context.PurchaseReturns.Update(document);
    }

    public async Task<PagedResult<SalesReturn>> QuerySalesReturnsAsync(
        PagingParams? paging = null,
        Expression<Func<SalesReturn, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<SalesReturn> query = context.SalesReturns
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }

    public async Task<PagedResult<PurchaseReturn>> QueryPurchaseReturnsAsync(
        PagingParams? paging = null,
        Expression<Func<PurchaseReturn, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PurchaseReturn> query = context.PurchaseReturns
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}