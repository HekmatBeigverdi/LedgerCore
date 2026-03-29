using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Models.Master;

public class PartyDto
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public PartyType Type { get; set; }
    public int? CategoryId { get; set; }
    public string? NationalId { get; set; }
    public string? EconomicCode { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public decimal? CreditLimit { get; set; }
    public int? DefaultCurrencyId { get; set; }
    public bool IsActive { get; set; }
}