using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Master;

public class Branch: AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public string? Phone { get; set; }

    public bool IsHeadOffice { get; set; } = false;
    public bool IsActive { get; set; } = true;
}