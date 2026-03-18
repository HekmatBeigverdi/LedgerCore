namespace LedgerCore.Core.Models.Security;

public class PermissionCategory
{
    public string Key { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public List<PermissionDefinition> Permissions { get; set; } = new();
}

public class PermissionDefinition
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}