using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class UserRepository(LedgerCoreDbContext context) : RepositoryBase<User>(context), IUserRepository
{
    public Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.UserName == userName, cancellationToken);
    }
}