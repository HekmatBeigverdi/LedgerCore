using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Security;

public class SecurityActivityLog : AuditableEntity
{
    public string Action { get; set; } = default!;       // e.g. "RolePermission.Assigned"
    public string EntityType { get; set; } = default!;   // e.g. "Role"
    public int EntityId { get; set; }                    // RoleId

    public int? ActorUserId { get; set; }
    public string? ActorUserName { get; set; }

    public string? Details { get; set; }                 // JSON or text
}