namespace LedgerCore.Core.ViewModels.Security;

public class PermissionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;      // مثل "Sales.Invoice.View"
    public string? Description { get; set; }
}