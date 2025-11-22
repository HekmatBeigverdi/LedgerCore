using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class UserRepository(LedgerCoreDbContext context) : RepositoryBase<User>(context), IUserRepository
{
    public Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.UserName == userName, cancellationToken);
    }

    public async Task<PagedResult<User>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = DbSet.AsNoTracking();
        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}