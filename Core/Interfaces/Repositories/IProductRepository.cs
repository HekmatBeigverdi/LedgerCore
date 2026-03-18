using System.Linq.Expressions;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<PagedResult<Product>> QueryAsync(PagingParams? paging = null,
        Expression<Func<Product, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}