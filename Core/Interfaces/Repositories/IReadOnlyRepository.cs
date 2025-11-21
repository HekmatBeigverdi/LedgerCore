using System.Linq.Expressions;
using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IReadOnlyRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // GetAll with optional paging (if pagingParams is null returns all)
    Task<PagedResult<TEntity>> GetAllAsync(PagingParams? pagingParams = null, 
        CancellationToken cancellationToken = default);

    // Find with predicate + optional paging
    Task<PagedResult<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        PagingParams? pagingParams = null,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
}