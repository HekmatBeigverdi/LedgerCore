using LedgerCore.Core.Constants;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Services;

public class PostingEngineService(
    IUnitOfWork uow,
    INumberSeriesService numberSeries) : IPostingEngineService
{
    public async Task<JournalVoucher> BuildJournalAsync(
        string documentType,
        int branchId,
        DateTime date,
        int refDocumentId,
        string? refDocumentNumber,
        PostingContext context,
        CancellationToken cancellationToken = default)
    {
        var ruleRepo = uow.Repository<PostingRule>();

        var page = await ruleRepo.FindAsync(
            x => x.DocumentType == documentType
                 && x.IsActive
                 && (x.BranchId == null || x.BranchId == branchId),
            null,
            cancellationToken);

        var rule = page.Items
            .OrderByDescending(x => x.BranchId.HasValue)
            .ThenByDescending(x => x.Priority)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"No posting rule defined for documentType={documentType}.");

        var lines = rule.Lines
            .Where(x => x.IsActive)
            .OrderBy(x => x.LineNumber)
            .ToList();

        if (lines.Count == 0)
            throw new InvalidOperationException($"Posting rule '{rule.Code}' has no active lines.");

        var fiscalPeriodId = await GetOpenFiscalPeriodIdAsync(date, cancellationToken);

        var journal = new JournalVoucher
        {
            Number = await numberSeries.NextAsync(NumberSeriesKeys.Journal, branchId, cancellationToken),
            Date = date,
            BranchId = branchId,
            FiscalPeriodId = fiscalPeriodId,
            Description = string.IsNullOrWhiteSpace(context.Description)
                ? $"{documentType} {refDocumentNumber}"
                : context.Description,
            Status = DocumentStatus.Posted,
            Lines = new List<JournalLine>()
        };

        foreach (var lineRule in lines)
        {
            var amount = ResolveAmount(lineRule.AmountSource, lineRule.FixedAmount, context);
            if (amount == 0m)
                continue;

            journal.Lines.Add(new JournalLine
            {
                LineNumber = lineRule.LineNumber,
                AccountId = lineRule.AccountId,
                Debit = lineRule.Side == PostingLineSide.Debit ? amount : 0m,
                Credit = lineRule.Side == PostingLineSide.Credit ? amount : 0m,
                PartyId = lineRule.UsePartyFromDocument ? context.PartyId : null,
                CurrencyId = context.CurrencyId,
                FxRate = context.FxRate,
                RefDocumentType = documentType,
                RefDocumentId = refDocumentId,
                Description = lineRule.DescriptionTemplate ?? journal.Description
            });
        }

        if (journal.Lines.Count == 0)
            throw new InvalidOperationException($"Posting rule '{rule.Code}' produced no journal lines.");

        return journal;
    }

    private static decimal ResolveAmount(
        PostingAmountSource source,
        decimal? fixedAmount,
        PostingContext context)
    {
        return source switch
        {
            PostingAmountSource.Total => context.Total,
            PostingAmountSource.Net => context.Net,
            PostingAmountSource.Tax => context.Tax,
            PostingAmountSource.Discount => context.Discount,
            PostingAmountSource.Gross => context.Gross,
            PostingAmountSource.TotalDeductions => context.TotalDeductions,
            PostingAmountSource.TotalNet => context.TotalNet,
            PostingAmountSource.DifferenceValue => Math.Abs(context.DifferenceValue),
            PostingAmountSource.FixedAmount => fixedAmount ?? 0m,
            _ => 0m
        };
    }

    private async Task<int> GetOpenFiscalPeriodIdAsync(DateTime date, CancellationToken ct)
    {
        var fyRepo = uow.Repository<FiscalYear>();
        var fyPage = await fyRepo.FindAsync(y => y.StartDate <= date && y.EndDate >= date, null, ct);

        var year = fyPage.Items
            .OrderByDescending(y => y.StartDate)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"No fiscal year found for date={date:yyyy-MM-dd}.");

        if (year.IsClosed)
            throw new InvalidOperationException($"Fiscal year '{year.Name}' is closed.");

        var fpRepo = uow.Repository<FiscalPeriod>();
        var fpPage = await fpRepo.FindAsync(
            p => p.FiscalYearId == year.Id && p.StartDate <= date && p.EndDate >= date,
            null,
            ct);

        var period = fpPage.Items
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"No fiscal period found for date={date:yyyy-MM-dd}.");

        if (period.IsClosed)
            throw new InvalidOperationException($"Fiscal period '{period.Name}' is closed.");

        return period.Id;
    }
}