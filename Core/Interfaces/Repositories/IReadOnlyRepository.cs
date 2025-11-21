using System.Linq.Expressions;
using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Interfaces.Repositories;

/// <summary>
/// Read-only repository abstraction with optional paging & filtering.
/// TEntity must inherit from BaseEntity.
/// </summary>
public interface IReadOnlyRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all entities with optional paging.
    /// If pagingParams is null, returns all records (in a PagedResult wrapper).
    /// </summary>
    Task<PagedResult<TEntity>> GetAllAsync(PagingParams? pagingParams = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find by predicate with optional paging.
    /// </summary>
    Task<PagedResult<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        PagingParams? pagingParams = null,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
}