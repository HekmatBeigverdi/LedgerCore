using LedgerCore.Core.Models.Security;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IRoleRepository: IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}