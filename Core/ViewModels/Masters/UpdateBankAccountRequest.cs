namespace LedgerCore.Core.ViewModels.Masters;

public class UpdateBankAccountRequest
{
    public string AccountNumber { get; set; } = default!;
    public string? Iban { get; set; }
    public string? Title { get; set; }
    public int? BankId { get; set; }
    public int? CurrencyId { get; set; }
    public bool IsActive { get; set; } = true;
}