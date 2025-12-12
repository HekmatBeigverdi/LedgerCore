namespace LedgerCore.Core.ViewModels.Security;

public class RolePermissionChangeRequest
{
    public List<int> PermissionIds { get; set; } = new();
}