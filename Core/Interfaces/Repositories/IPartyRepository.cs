using System.Linq.Expressions;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Interfaces.Repositories;

/// <summary>
/// Repository for Party (customer/supplier) master data.
/// </summary>
public interface IPartyRepository : IRepository<Party>
{
    Task<Party?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query with optional predicate and paging.
    /// </summary>
    Task<PagedResult<Party>> QueryAsync(PagingParams? paging = null,
        Expression<Func<Party, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}