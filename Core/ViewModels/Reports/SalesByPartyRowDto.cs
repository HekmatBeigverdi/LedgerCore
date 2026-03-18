namespace LedgerCore.Core.ViewModels.Reports;

public class SalesByPartyRowDto
{
    public int PartyId { get; set; }
    public string PartyCode { get; set; } = default!;
    public string PartyName { get; set; } = default!;

    public decimal TotalAmount { get; set; }
}