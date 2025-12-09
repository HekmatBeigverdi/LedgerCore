using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Documents;

public class CashTransferDto
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string Number { get; set; } = null!;

    public int FromAccountId { get; set; }
    public string FromAccountName { get; set; } = null!;

    public int ToAccountId { get; set; }
    public string ToAccountName { get; set; } = null!;

    public decimal Amount { get; set; }

    public int CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = null!;

    public decimal FxRate { get; set; }

    public string? Description { get; set; }

    public DocumentStatus Status { get; set; }

    public int? JournalId { get; set; }
}

public class CashTransferCreateDto
{
    public DateTime Date { get; set; }

    public string? Number { get; set; }

    public int FromAccountId { get; set; }

    public int ToAccountId { get; set; }

    public decimal Amount { get; set; }

    public int CurrencyId { get; set; }

    public decimal FxRate { get; set; } = 1m;

    public string? Description { get; set; }
}

public class CashTransferListItemDto
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string Number { get; set; } = null!;

    public string FromAccountName { get; set; } = null!;

    public string ToAccountName { get; set; } = null!;

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public DocumentStatus Status { get; set; }
}