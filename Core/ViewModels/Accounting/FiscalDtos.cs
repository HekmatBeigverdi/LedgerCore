namespace LedgerCore.Core.ViewModels.Accounting;

public class FiscalYearDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class FiscalPeriodDto
{
    public int Id { get; set; }
    public int FiscalYearId { get; set; }
    public string? FiscalYearName { get; set; }

    public string Name { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
}

// ===== Requests =====

public class CreateFiscalYearRequest
{
    public string Name { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpdateFiscalYearRequest : CreateFiscalYearRequest
{
    public int Id { get; set; }
}

public class CreateFiscalPeriodRequest
{
    public int FiscalYearId { get; set; }
    public string Name { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpdateFiscalPeriodRequest : CreateFiscalPeriodRequest
{
    public int Id { get; set; }
}

/// <summary>
/// برای باز کردن دوره (Re-open)
/// </summary>
public class OpenFiscalPeriodRequest
{
    public int FiscalPeriodId { get; set; }
}