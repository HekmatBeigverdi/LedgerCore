namespace LedgerCore.Core.ViewModels.Security;

public class RoleCreateUpdateRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public List<int> PermissionIds { get; set; } = new();
}