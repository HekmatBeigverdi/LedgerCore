using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Master;

public class BankAccount: AuditableEntity
{
    public string AccountNumber { get; set; } = default!;
    public string? Iban { get; set; }
    public string? Title { get; set; }           // نام روی حساب

    public int? BankId { get; set; }
    public Bank? Bank { get; set; }

    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public bool IsActive { get; set; } = true;
}