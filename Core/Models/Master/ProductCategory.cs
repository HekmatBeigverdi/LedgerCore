using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Master;

public class ProductCategory: AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}