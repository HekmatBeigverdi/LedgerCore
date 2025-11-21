using System.Linq.Expressions;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IPartyRepository : IRepository<Party>
{
    Task<Party?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    // list with optional filter/paging - filter DTO can be expanded later
    Task<PagedResult<Party>> QueryAsync(PagingParams? paging = null, 
        Expression<Func<Party, bool>>? additionalPredicate = null,
        CancellationToken cancellationToken = default);
}