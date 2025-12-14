using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.ViewModels.Security;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/security/logs")]
public class SecurityLogsController(IUnitOfWork uow) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SecurityActivityLogDto>>> Get(
        [FromQuery] int? roleId,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<SecurityActivityLog>();

        var logs = roleId.HasValue
            ? await repo.FindAsync(x => x.EntityType == "Role" && x.EntityId == roleId.Value, null, cancellationToken)
            : await repo.GetAllAsync(null, cancellationToken);

        var dto = logs.Items
            .OrderByDescending(x => x.CreatedAt)
            .Take(500)
            .Select(x => new SecurityActivityLogDto
            {
                Id = x.Id,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                ActorUserId = x.ActorUserId,
                ActorUserName = x.ActorUserName,
                Details = x.Details,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        return Ok(dto);
    }
}
