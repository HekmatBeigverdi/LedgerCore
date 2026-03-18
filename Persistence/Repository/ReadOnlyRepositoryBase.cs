using System.Linq.Expressions;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class ReadOnlyRepositoryBase<TEntity>(LedgerCoreDbContext context) : IReadOnlyRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly LedgerCoreDbContext Context = context;
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<PagedResult<TEntity>> GetAllAsync(PagingParams? pagingParams = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = DbSet.AsNoTracking();
        return await QueryHelpers.ApplyPagingAsync(query, pagingParams, cancellationToken);
    }

    public virtual async Task<PagedResult<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        PagingParams? pagingParams = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = DbSet.AsNoTracking().Where(predicate);
        return await QueryHelpers.ApplyPagingAsync(query, pagingParams, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(predicate, cancellationToken);
    }
}