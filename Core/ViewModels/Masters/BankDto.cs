namespace LedgerCore.Core.ViewModels.Masters;

public class BankDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
}