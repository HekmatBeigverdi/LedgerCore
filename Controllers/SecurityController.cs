using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.ViewModels.Security;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/security")]
public class SecurityController : ControllerBase
{
    /// <summary>
    /// ساختار Permissionها برای UI (گروه‌بندی شده).
    /// </summary>
    [HttpGet("permissions-tree")]
    [HasPermission(PermissionCodes.Security_Permissions_View)]
    public ActionResult<IEnumerable<PermissionCategory>> GetPermissionsTree()
    {
        var tree = PermissionStructure.GetCategories();
        return Ok(tree);
    }

    [HttpGet("roles")]
    [HasPermission(PermissionCodes.Security_Roles_View)]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles(
        [FromServices] IUnitOfWork uow,
        CancellationToken cancellationToken)
    {
        var roles = await uow.Repository<Role>().GetAllAsync(cancellationToken: cancellationToken);
        return Ok(roles.Items.Select(r => new
        {
            r.Id,
            r.Name,
            r.Description,
            r.IsSystemRole
        }));
    }

    [HttpGet("roles/{roleId:int}/permissions")]
    [HasPermission(PermissionCodes.Security_Roles_View)]
    public async Task<ActionResult<IEnumerable<string>>> GetRolePermissions(
        int roleId,
        [FromServices] IUnitOfWork uow,
        CancellationToken cancellationToken)
    {
        var rp = await uow.Repository<RolePermission>()
            .FindAsync(r => r.RoleId == roleId, cancellationToken: cancellationToken);

        var permissions = await uow.Repository<Permission>().GetAllAsync(cancellationToken: cancellationToken);

        var assigned = permissions.Items
            .Where(p => rp.Items.Any(x => x.PermissionId == p.Id))
            .Select(p => p.Code)
            .ToList();

        return Ok(assigned);
    }
}