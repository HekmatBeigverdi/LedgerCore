using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class RoleRepository(LedgerCoreDbContext context) : RepositoryBase<Role>(context), IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }
}