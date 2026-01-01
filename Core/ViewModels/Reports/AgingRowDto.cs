namespace LedgerCore.Core.ViewModels.Reports;

public class AgingRowDto
{
    public int PartyId { get; set; }
    public string PartyCode { get; set; } = default!;
    public string PartyName { get; set; } = default!;
    public string PartyType { get; set; } = default!;

    // Receivable (Net > 0)
    public decimal Current_0_30 { get; set; }
    public decimal Due_31_60 { get; set; }
    public decimal Due_61_90 { get; set; }
    public decimal Due_91_120 { get; set; }
    public decimal Due_121_Plus { get; set; }
    public decimal TotalReceivable { get; set; }

    // Payable (Net < 0)
    public decimal Pay_Current_0_30 { get; set; }
    public decimal Pay_31_60 { get; set; }
    public decimal Pay_61_90 { get; set; }
    public decimal Pay_91_120 { get; set; }
    public decimal Pay_121_Plus { get; set; }
    public decimal TotalPayable { get; set; }
}