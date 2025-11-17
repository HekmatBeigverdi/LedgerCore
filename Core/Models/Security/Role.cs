using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Security;

public class Role: AuditableEntity
{
    public string Name { get; set; } = default!;        // Admin, Accountant, ...
    public string? Description { get; set; }

    public bool IsSystemRole { get; set; } = false;     // نقش‌های سیستمی که حذف‌شان محدود است

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}