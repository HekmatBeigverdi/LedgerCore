using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace LedgerCore.Persistence.Repository;

public class UnitOfWork(
    LedgerCoreDbContext context,
    IPartyRepository parties,
    IProductRepository products,
    IInvoiceRepository invoices,
    IJournalRepository journals,
    IStockRepository stock,
    IChequeRepository cheques,
    IFixedAssetRepository fixedAssets,
    IPayrollRepository payrolls,
    IUserRepository users,
    IRoleRepository roles)
    : IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public IPartyRepository Parties { get; } = parties;
    public IProductRepository Products { get; } = products;
    public IInvoiceRepository Invoices { get; } = invoices;
    public IJournalRepository Journals { get; } = journals;
    public IStockRepository Stock { get; } = stock;
    public IChequeRepository Cheques { get; } = cheques;
    public IFixedAssetRepository FixedAssets { get; } = fixedAssets;
    public IPayrollRepository Payrolls { get; } = payrolls;
    public IUserRepository Users { get; } = users;
    public IRoleRepository Roles { get; } = roles;

    public IRepository<T> Repository<T>() where T : BaseEntity           
    {
        // برای سادگی، می‌توانیم از سرویس DI استفاده کنیم؛
        // اما در اینجا فرض می‌کنیم هرکس نیاز دارد، مستقیم از DI می‌گیرد.
        throw new NotImplementedException("Generic repository resolution is not implemented here.");
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
            return;

        _currentTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
            return;

        await context.SaveChangesAsync(cancellationToken);
        await _currentTransaction.CommitAsync(cancellationToken);

        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
            return;

        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }

        await context.DisposeAsync();
    }
}