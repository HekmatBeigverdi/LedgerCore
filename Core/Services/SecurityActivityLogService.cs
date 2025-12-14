using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Security;

namespace LedgerCore.Core.Services;

public class SecurityActivityLogService(IUnitOfWork uow) : ISecurityActivityLogService
{
    public async Task LogAsync(
        string action,
        string entityType,
        int entityId,
        int? actorUserId,
        string? actorUserName,
        string? details,
        CancellationToken cancellationToken = default)
    {
        var repo = uow.Repository<SecurityActivityLog>();
        await repo.AddAsync(new SecurityActivityLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            Details = details
        }, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);
    }
}