using LedgerCore.Core.Constants;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Master;

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
        var lineRepo = uow.Repository<PostingRuleLine>();

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

        var linePage = await lineRepo.FindAsync(
            x => x.PostingRuleId == rule.Id && x.IsActive,
            null,
            cancellationToken);

        var lines = linePage.Items
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

        await ValidateGeneratedJournalAsync(journal, cancellationToken);

        return journal;
    }
    private async Task ValidateGeneratedJournalAsync(
        JournalVoucher journal,
        CancellationToken ct)
    {
        if (journal is null)
            throw new InvalidOperationException("Generated journal is required.");

        if (journal.Date == default)
            throw new InvalidOperationException("Generated journal date is required.");

        if (journal.BranchId <= 0)
            throw new InvalidOperationException("Generated journal BranchId is required.");

        if (!journal.FiscalPeriodId.HasValue || journal.FiscalPeriodId.Value <= 0)
            throw new InvalidOperationException("Generated journal FiscalPeriodId is required.");

        if (journal.Lines is null || journal.Lines.Count == 0)
            throw new InvalidOperationException("Generated journal must have at least one line.");

        await ValidateGeneratedJournalLinesAsync(journal.Lines, ct);
        EnsureBalanced(journal.Lines);
    }
    private async Task ValidateGeneratedJournalLinesAsync(IEnumerable<JournalLine> lines, CancellationToken ct)
    {
        var accountCache = new Dictionary<int, Account>();
        var partyCache = new Dictionary<int, Party>();

        var index = 0;
        foreach (var line in lines.OrderBy(x => x.LineNumber))
        {
            index++;

            if (line.LineNumber <= 0)
                throw new InvalidOperationException($"Generated line[{index}]: LineNumber is required.");

            if (line.AccountId <= 0)
                throw new InvalidOperationException($"Generated line[{index}]: AccountId is required.");

            if (line.Debit < 0 || line.Credit < 0)
                throw new InvalidOperationException($"Generated line[{index}]: Debit/Credit cannot be negative.");

            if (line.Debit > 0 && line.Credit > 0)
                throw new InvalidOperationException($"Generated line[{index}]: A line cannot have both Debit and Credit.");

            if (line.Debit == 0 && line.Credit == 0)
                throw new InvalidOperationException($"Generated line[{index}]: Either Debit or Credit must be greater than zero.");

            if (!accountCache.TryGetValue(line.AccountId, out var account))
            {
                account = await uow.Accounts.GetByIdAsync(line.AccountId, ct)
                          ?? throw new InvalidOperationException(
                              $"Generated line[{index}]: Account not found (Id={line.AccountId}).");

                accountCache[line.AccountId] = account;
            }

            if (!account.IsActive)
                throw new InvalidOperationException(
                    $"Generated line[{index}]: Account is inactive (Code={account.Code}).");

            if (!account.IsPosting)
                throw new InvalidOperationException(
                    $"Generated line[{index}]: Account is not posting (Code={account.Code}).");

            await ValidateAccountPartyRulesAsync(index, line, account, partyCache, ct);

            if (line.CurrencyId.HasValue && line.CurrencyId.Value <= 0)
                throw new InvalidOperationException($"Generated line[{index}]: CurrencyId is invalid.");

            if (line.FxRate <= 0)
                throw new InvalidOperationException($"Generated line[{index}]: FxRate must be greater than zero.");
        }
    }
    private async Task ValidateAccountPartyRulesAsync(
        int index,
        JournalLine line,
        Account account,
        Dictionary<int, Party> partyCache,
        CancellationToken ct)
    {
        if (account.RequiresParty && line.PartyId is null)
        {
            throw new InvalidOperationException(
                $"Generated line[{index}]: Party is required for account {account.Code} - {account.Name}.");
        }

        if (line.PartyId is null)
            return;

        var partyId = line.PartyId.Value;

        if (!partyCache.TryGetValue(partyId, out var party))
        {
            party = await uow.Parties.GetByIdAsync(partyId, ct)
                    ?? throw new InvalidOperationException(
                        $"Generated line[{index}]: Party not found (Id={partyId}).");

            partyCache[partyId] = party;
        }

        if (!party.IsActive)
        {
            throw new InvalidOperationException(
                $"Generated line[{index}]: Party is inactive (Code={party.Code}).");
        }

        if (account.AllowedPartyType.HasValue && party.Type != account.AllowedPartyType.Value)
        {
            throw new InvalidOperationException(
                $"Generated line[{index}]: Party type '{party.Type}' is not allowed for account {account.Code} - {account.Name}.");
        }
    }
    private static void EnsureBalanced(IEnumerable<JournalLine> lines)
    {
        var totalDebit = lines.Sum(x => x.Debit);
        var totalCredit = lines.Sum(x => x.Credit);

        if (totalDebit != totalCredit)
        {
            throw new InvalidOperationException(
                $"Generated journal is not balanced. TotalDebit={totalDebit}, TotalCredit={totalCredit}.");
        }
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