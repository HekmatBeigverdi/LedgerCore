namespace LedgerCore.Core.Models.Enums;

public enum PostingAmountSource
{
    Total = 1,
    Net = 2,
    Tax = 3,
    Discount = 4,
    Gross = 5,
    TotalDeductions = 6,
    TotalNet = 7,
    DifferenceValue = 8,
    FixedAmount = 9
}