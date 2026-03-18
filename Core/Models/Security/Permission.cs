using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Security;

public class Permission: AuditableEntity
{
    public string Code { get; set; } = default!;        // مثلا "Invoices_View", "Invoices_Edit"
    public string Name { get; set; } = default!;        // عنوان خوانا
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}