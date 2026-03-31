namespace LedgerCore.Core.ViewModels.Masters;

public class CreateBankRequest
{
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
}