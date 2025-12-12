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
        await SeedRolePermissionsAsync(uow, cancellationToken); // Admin = all permissions
        await SeedRolePermissionForCustomRoles(uow, cancellationToken); // نقش‌های جدید
        await SeedAdminUserAsync(uow, cancellationToken: cancellationToken);
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
    public static async Task SeedRolePermissionForCustomRoles(
        IUnitOfWork uow,
        CancellationToken cancellationToken = default)
    {
        var roleRepo = uow.Repository<Role>();
        var permRepo = uow.Repository<Permission>();
        var rpRepo = uow.Repository<RolePermission>();

        // توجه: امضای GetAllAsync در پروژه شما (PagingParams? pagingParams = null, CancellationToken ct = default) است
        var rolesPage = await roleRepo.GetAllAsync(null, cancellationToken);
        var permsPage = await permRepo.GetAllAsync(null, cancellationToken);

        var roles = rolesPage.Items;
        var permissions = permsPage.Items;

        var accountant = roles.FirstOrDefault(r => r.Name == RoleSeedData.Accountant);
        var inventoryManager = roles.FirstOrDefault(r => r.Name == RoleSeedData.InventoryManager);
        var auditor = roles.FirstOrDefault(r => r.Name == RoleSeedData.Auditor);

        if (accountant != null)
        {
            var allowed = permissions.Where(p =>
                p.Code.StartsWith("Accounting.") ||
                p.Code.StartsWith("Reports.")
            );

            foreach (var perm in allowed)
            {
                var exists = await rpRepo.AnyAsync(
                    rp => rp.RoleId == accountant.Id && rp.PermissionId == perm.Id,
                    cancellationToken);

                if (exists) continue;

                await rpRepo.AddAsync(new RolePermission
                {
                    RoleId = accountant.Id,
                    PermissionId = perm.Id
                }, cancellationToken);
            }
        }

        if (inventoryManager != null)
        {
            var allowed = permissions.Where(p => p.Code.StartsWith("Inventory."));

            foreach (var perm in allowed)
            {
                var exists = await rpRepo.AnyAsync(
                    rp => rp.RoleId == inventoryManager.Id && rp.PermissionId == perm.Id,
                    cancellationToken);

                if (exists) continue;

                await rpRepo.AddAsync(new RolePermission
                {
                    RoleId = inventoryManager.Id,
                    PermissionId = perm.Id
                }, cancellationToken);
            }
        }

        if (auditor != null)
        {
            // فقط View ها
            var allowed = permissions.Where(p => p.Code.EndsWith(".View"));

            foreach (var perm in allowed)
            {
                var exists = await rpRepo.AnyAsync(
                    rp => rp.RoleId == auditor.Id && rp.PermissionId == perm.Id,
                    cancellationToken);

                if (exists) continue;

                await rpRepo.AddAsync(new RolePermission
                {
                    RoleId = auditor.Id,
                    PermissionId = perm.Id
                }, cancellationToken);
            }
        }

        await uow.SaveChangesAsync(cancellationToken);
    }
    public static async Task SeedAdminUserAsync(
        IUnitOfWork uow,
        string adminUserName = "admin",
        string defaultPassword = "Admin@12345",
        CancellationToken cancellationToken = default)
    {
        var userRepo = uow.Repository<User>();
        var roleRepo = uow.Repository<Role>();
        var urRepo = uow.Repository<UserRole>();

        // اگر admin موجود است، کاری نکن
        var exists = await userRepo.AnyAsync(u => u.UserName == adminUserName, cancellationToken);
        if (exists) return;

        var adminRolePage = await roleRepo.FindAsync(r => r.Name == RoleSeedData.AdminRoleName, null, cancellationToken);
        var adminRole = adminRolePage.Items.FirstOrDefault();
        if (adminRole is null)
            throw new InvalidOperationException("Admin role not found. Run role seeding first.");

        AuthService.CreatePasswordHash(defaultPassword, out var hash, out var salt);

        var admin = new User
        {
            UserName = adminUserName,
            DisplayName = "System Admin",
            Email = "admin@local",
            PasswordHash = hash,
            PasswordSalt = salt,
            Status = Core.Models.Enums.UserStatus.Active
        };

        await userRepo.AddAsync(admin, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken); // تا Id تولید شود

        await urRepo.AddAsync(new UserRole
        {
            UserId = admin.Id,
            RoleId = adminRole.Id
        }, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);
    }

}
