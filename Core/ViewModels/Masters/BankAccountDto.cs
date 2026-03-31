namespace LedgerCore.Core.ViewModels.Masters;

public class BankAccountDto
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = default!;
    public string? Iban { get; set; }
    public string? Title { get; set; }
    public int? BankId { get; set; }
    public string? BankName { get; set; }
    public int? CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public bool IsActive { get; set; }
}