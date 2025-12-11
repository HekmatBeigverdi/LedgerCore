using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Accounting;

public class JournalVoucherDto
{
    public int Id { get; set; }
    public string Number { get; set; } = default!;
    public DateTime Date { get; set; }
    public string? Description { get; set; }

    public DocumentStatus Status { get; set; }

    public int? BranchId { get; set; }
    public string? BranchName { get; set; }

    public int? FiscalPeriodId { get; set; }
    public string? FiscalPeriodName { get; set; }

    public List<JournalLineDto> Lines { get; set; } = new();
}

public class JournalLineDto
{
    public int Id { get; set; }

    public int AccountId { get; set; }
    public string? AccountCode { get; set; }
    public string? AccountName { get; set; }

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    public int? PartyId { get; set; }
    public int? CostCenterId { get; set; }
    public int? ProjectId { get; set; }
    public int? CurrencyId { get; set; }
    public decimal FxRate { get; set; }

    public string? Description { get; set; }
    public string? RefDocumentType { get; set; }
    public int? RefDocumentId { get; set; }

    public int LineNumber { get; set; }
}

public class CreateJournalVoucherRequest
{
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }

    public int? BranchId { get; set; }
    public int? FiscalPeriodId { get; set; }

    public List<CreateJournalLineRequest> Lines { get; set; } = new();
}

public class CreateJournalLineRequest
{
    public int AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    public int? PartyId { get; set; }
    public int? CostCenterId { get; set; }
    public int? ProjectId { get; set; }
    public int? CurrencyId { get; set; }
    public decimal FxRate { get; set; } = 1m;

    public string? Description { get; set; }
    public string? RefDocumentType { get; set; }
    public int? RefDocumentId { get; set; }

    public int LineNumber { get; set; }
}

public class UpdateJournalVoucherRequest : CreateJournalVoucherRequest
{
    public int Id { get; set; }
}

public class CloseFiscalPeriodRequest
{
    public int FiscalPeriodId { get; set; }

    /// <summary>
    /// شناسه حساب Equity که سود/زیان دوره به آن منتقل می‌شود
    /// </summary>
    public int ProfitAndLossAccountId { get; set; }
}