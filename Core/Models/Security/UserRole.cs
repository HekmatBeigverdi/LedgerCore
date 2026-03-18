using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Security;

public class UserRole: BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int RoleId { get; set; }
    public Role? Role { get; set; }
}