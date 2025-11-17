using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IPartyRepository: IRepository<Party>
{
    Task<Party?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}