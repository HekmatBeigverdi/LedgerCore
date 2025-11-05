using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IUnitOfWork: IAsyncDisposable
{
    IPartyRepository Parties { get; }
    IProductRepository Products { get; }
    IInvoiceRepository Invoices { get; }
    IJournalRepository Journals { get; }
    IStockRepository Stock { get; }
    IChequeRepository Cheques { get; }
    IFixedAssetRepository FixedAssets { get; }
    IPayrollRepository Payrolls { get; }
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }

    // اگر خواستی: IGeneric Repository
    IRepository<T> Repository<T>() where T : BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}