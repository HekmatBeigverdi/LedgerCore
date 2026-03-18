namespace LedgerCore.Core.ViewModels.Security;

public class UserCreateUpdateRequest
{
    public string UserName { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }

    public string? Password { get; set; }          // برای ایجاد یا تغییر رمز
    public bool IsActive { get; set; } = true;

    public List<int> RoleIds { get; set; } = new();
}