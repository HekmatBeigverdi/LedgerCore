using System.Linq.Expressions;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class ProductRepository(LedgerCoreDbContext context) : RepositoryBase<Product>(context), IProductRepository
{
    public Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<PagedResult<Product>> QueryAsync(PagingParams? paging = null,
        Expression<Func<Product, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = DbSet
            .Include(x => x.Category)
            .Include(x => x.DefaultTaxRate)
            .AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}