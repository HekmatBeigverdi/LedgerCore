using Microsoft.AspNetCore.Authorization;

namespace LedgerCore.Core.Models.Security;

/// <summary>
/// استفاده: [HasPermission("Sales.Invoice.View")]
/// </summary>
public class HasPermissionAttribute : AuthorizeAttribute
{
    private const string PolicyPrefix = "Permission";

    public HasPermissionAttribute(string permissionCode)
    {
        Policy = $"{PolicyPrefix}:{permissionCode}";
    }

    public static string BuildPolicyName(string permissionCode)
        => $"{PolicyPrefix}:{permissionCode}";
}