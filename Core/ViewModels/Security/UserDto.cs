namespace LedgerCore.Core.ViewModels.Security;

public class UserDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }

    public List<int> RoleIds { get; set; } = new();
}