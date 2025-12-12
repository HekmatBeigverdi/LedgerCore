namespace LedgerCore.Core.ViewModels.Security;

public class RoleManagementDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = default!;
    public string? RoleDescription { get; set; }

    public List<PermissionDto> Assigned { get; set; } = new();
    public List<PermissionDto> Unassigned { get; set; } = new();
}