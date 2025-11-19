using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;

namespace LedgerCore.Persistence.Repository;

public class RepositoryBase<TEntity>(LedgerCoreDbContext context)
    : ReadOnlyRepositoryBase<TEntity>(context), IRepository<TEntity>
    where TEntity : BaseEntity
{
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        DbSet.UpdateRange(entities);
    }

    public virtual void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        DbSet.RemoveRange(entities);
    }
}