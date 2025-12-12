using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Security;

namespace LedgerCore.Core.Services;

public static class SecuritySeeder
{
    public static async Task SeedAsync(
        IUnitOfWork uow,
        CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(uow, cancellationToken);
        await SeedRolesAsync(uow, cancellationToken);
        await SeedRolePermissionsAsync(uow, cancellationToken);
    }

    public static async Task SeedPermissionsAsync(
        IUnitOfWork uow,
        CancellationToken cancellationToken = default)
    {
        var permissionRepo = uow.Repository<Permission>();
        var all = PermissionSeedData.GetAll();

        foreach (var perm in all)
        {
            var exists = await permissionRepo.AnyAsync(
                p => p.Code == perm.Code,
                cancellationToken);

            if (exists)
                continue;

            await permissionRepo.AddAsync(new Permission
            {
                Code = perm.Code,
                Name = perm.Name,
                Description = perm.Description
            }, cancellationToken);
        }

        await uow.SaveChangesAsync(cancellationToken);
    }

    public static async Task SeedRolesAsync(
        IUnitOfWork uow,
        CancellationToken cancellationToken = default)
    {
        var roleRepo = uow.Repository<Role>();
        var allRoles = RoleSeedData.GetAll();

        foreach (var role in allRoles)
        {
            var exists = await roleRepo.AnyAsync(
                r => r.Name == role.Name,
                cancellationToken);

            if (exists)
                continue;

            await roleRepo.AddAsync(new Role
            {
                Name = role.Name,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole
            }, cancellationToken);
        }

        await uow.SaveChangesAsync(cancellationToken);
    }

    public static async Task SeedRolePermissionsAsync(
        IUnitOfWork uow,
        CancellationToken cancellationToken = default)
    {
        var roleRepo = uow.Repository<Role>();
        var permissionRepo = uow.Repository<Permission>();
        var rolePermissionRepo = uow.Repository<RolePermission>();

        // Admin role: تمام Permission ها را دارد
        var adminRolePage = await roleRepo.FindAsync(
            r => r.Name == RoleSeedData.AdminRoleName,
            pagingParams: null,
            cancellationToken);

        var adminRole = adminRolePage.Items.FirstOrDefault();
        if (adminRole is null)
            return; // نقش Admin هنوز ساخته نشده

        var allPermissionsPage = await permissionRepo.GetAllAsync(
            pagingParams: null,
            cancellationToken);

        foreach (var perm in allPermissionsPage.Items)
        {
            var exists = await rolePermissionRepo.AnyAsync(
                rp => rp.RoleId == adminRole.Id && rp.PermissionId == perm.Id,
                cancellationToken);

            if (exists)
                continue;

            await rolePermissionRepo.AddAsync(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = perm.Id
            }, cancellationToken);
        }

        await uow.SaveChangesAsync(cancellationToken);
    }
}
