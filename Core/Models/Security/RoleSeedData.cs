using System.Collections.Generic;

namespace LedgerCore.Core.Models.Security;

public static class RoleSeedData
{
    public const string AdminRoleName = "Admin";
    public const string Accountant = "Accountant";
    public const string InventoryManager = "InventoryManager";
    public const string Auditor = "Auditor";

    public static IReadOnlyList<Role> GetAll()
        => new List<Role>
        {
            new()
            {
                Name = AdminRoleName,
                Description = "نقش مدیر سیستم با دسترسی کامل به تمام امکانات",
                IsSystemRole = true
            },
            new()
            {
                Name = Accountant,
                Description = "حسابدار - دسترسی کامل به عملیات و گزارشات مالی",
                IsSystemRole = false
            },
            new()
            {
                Name = InventoryManager,
                Description = "مدیر انبار - دسترسی کامل به عملیات انبار",
                IsSystemRole = false
            },
            new()
            {
                Name = Auditor,
                Description = "ممیز / بازرس - فقط مشاهده",
                IsSystemRole = false
            }
        };
}