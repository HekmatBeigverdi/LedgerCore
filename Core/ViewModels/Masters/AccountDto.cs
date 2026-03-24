using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Masters;

public class AccountDto
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public AccountType Type { get; set; }
    public BalanceSide NormalSide { get; set; }
    public int Level { get; set; }
    public bool IsPosting { get; set; }
    public int? ParentAccountId { get; set; }
    public bool RequiresParty { get; set; }
    public PartyType? AllowedPartyType { get; set; }
    public bool IsActive { get; set; }
}