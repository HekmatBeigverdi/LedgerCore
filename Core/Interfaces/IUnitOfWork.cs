using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Interfaces;

/// <summary>
/// UnitOfWork for coordinating repositories & transactions.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
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

    IBranchRepository Branches { get; }
    ICostCenterRepository CostCenters { get; }
    IProjectRepository Projects { get; }
    ICurrencyRepository Currencies { get; }
    ITaxRateRepository TaxRates { get; }
    IWarehouseRepository Warehouses { get; }
    IReceiptRepository Receipts { get; }
    IPaymentRepository Payments { get; }
    IAccountRepository Accounts { get; }

    /// <summary>
    /// convenience: resolve a repository generically (optional).
    /// Implementation may throw NotImplementedException if not supported.
    /// </summary>
    IRepository<T> Repository<T>() where T : BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}