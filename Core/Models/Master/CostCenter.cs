using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Master;

public class CostCenter: AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}