using System.Collections.Generic;

namespace LedgerCore.Core.Models.Security;

public static class RoleSeedData
{
    public const string AdminRoleName = "Admin";

    public static IReadOnlyList<Role> GetAll()
        => new List<Role>
        {
            new()
            {
                Name = AdminRoleName,
                Description = "نقش مدیر سیستم با دسترسی کامل به تمام امکانات",
                IsSystemRole = true
            }
        };
}