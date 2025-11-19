using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Master;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class PartyRepository(LedgerCoreDbContext context) : RepositoryBase<Party>(context), IPartyRepository
{
    public Task<Party?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }
}