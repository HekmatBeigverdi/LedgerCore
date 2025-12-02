using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.ViewModels.Reports;
using LedgerCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Core.Services;

public class ReportingService(LedgerCoreDbContext db) : IReportingService
{
    #region Trial Balance

    public async Task<IReadOnlyList<TrialBalanceRow>> GetTrialBalanceAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        var baseLines = db.JournalLines
            .Include(l => l.Account)
            .Include(l => l.JournalVoucher)
            .Where(l => l.JournalVoucher != null &&
                        l.JournalVoucher.Status == DocumentStatus.Posted);

        if (branchId.HasValue)
        {
            baseLines = baseLines.Where(l => l.JournalVoucher!.BranchId == branchId.Value);
        }

        // مانده تا قبل از ابتدای دوره = مانده اول دوره
        var openingQuery = baseLines.Where(l => l.JournalVoucher!.Date < fromDate);

        // گردش داخل دوره
        var periodQuery = baseLines.Where(l =>
            l.JournalVoucher!.Date >= fromDate &&
            l.JournalVoucher!.Date <= toDate);

        var opening = await openingQuery
            .GroupBy(l => new { l.AccountId, l.Account!.Code, l.Account!.Name })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.Code,
                g.Key.Name,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            })
            .ToListAsync(cancellationToken);

        var period = await periodQuery
            .GroupBy(l => new { l.AccountId, l.Account!.Code, l.Account!.Name })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.Code,
                g.Key.Name,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<int, TrialBalanceRow>();

        TrialBalanceRow GetOrCreate(int accountId, string code, string name)
        {
            if (!result.TryGetValue(accountId, out var row))
            {
                row = new TrialBalanceRow
                {
                    AccountId = accountId,
                    AccountCode = code,
                    AccountName = name
                };
                result[accountId] = row;
            }

            return row;
        }

        // Opening
        foreach (var o in opening)
        {
            var row = GetOrCreate(o.AccountId, o.Code, o.Name);
            var net = o.Debit - o.Credit; // مانده = بدهکار - بستانکار

            if (net > 0)
            {
                row.OpeningDebit = net;
            }
            else if (net < 0)
            {
                row.OpeningCredit = Math.Abs(net);
            }
        }

        // Period
        foreach (var p in period)
        {
            var row = GetOrCreate(p.AccountId, p.Code, p.Name);
            var net = p.Debit - p.Credit;

            if (net > 0)
            {
                row.PeriodDebit = net;
            }
            else if (net < 0)
            {
                row.PeriodCredit = Math.Abs(net);
            }
        }

        return result.Values
            .OrderBy(r => r.AccountCode)
            .ToList();
    }

    #endregion

    #region General Ledger

    public async Task<IReadOnlyList<GeneralLedgerRowDto>> GetGeneralLedgerAsync(
        DateTime fromDate,
        DateTime toDate,
        int? accountId,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        var baseLines = db.JournalLines
            .Include(l => l.Account)
            .Include(l => l.JournalVoucher)
            .Where(l => l.JournalVoucher != null &&
                        l.JournalVoucher.Status == DocumentStatus.Posted);

        if (branchId.HasValue)
            baseLines = baseLines.Where(l => l.JournalVoucher!.BranchId == branchId.Value);

        if (accountId.HasValue)
            baseLines = baseLines.Where(l => l.AccountId == accountId.Value);

        var openingQuery = baseLines.Where(l => l.JournalVoucher!.Date < fromDate);
        var periodQuery = baseLines.Where(l =>
            l.JournalVoucher!.Date >= fromDate &&
            l.JournalVoucher!.Date <= toDate);

        var opening = await openingQuery
            .GroupBy(l => new { l.AccountId })
            .Select(g => new
            {
                g.Key.AccountId,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            })
            .ToListAsync(cancellationToken);

        var openingDict = opening.ToDictionary(
            x => x.AccountId,
            x => x.Debit - x.Credit); // مانده اول دوره (بدهکار - بستانکار)

        var lines = await periodQuery
            .Select(l => new
            {
                l.AccountId,
                AccountCode = l.Account!.Code,
                AccountName = l.Account!.Name,
                Date = l.JournalVoucher!.Date,
                JournalNumber = l.JournalVoucher!.Number,
                l.Debit,
                l.Credit,
                Description = l.Description ?? l.JournalVoucher!.Description
            })
            .OrderBy(x => x.AccountCode)
            .ThenBy(x => x.Date)
            .ThenBy(x => x.JournalNumber)
            .ToListAsync(cancellationToken);

        var result = new List<GeneralLedgerRowDto>();

        var grouped = lines.GroupBy(x => new { x.AccountId, x.AccountCode, x.AccountName });

        foreach (var g in grouped)
        {
            var accountIdKey = g.Key.AccountId;
            var runningNet = openingDict.TryGetValue(accountIdKey, out var open)
                ? open
                : 0m;

            foreach (var l in g)
            {
                runningNet += l.Debit - l.Credit;

                var row = new GeneralLedgerRowDto
                {
                    AccountId = g.Key.AccountId,
                    AccountCode = g.Key.AccountCode,
                    AccountName = g.Key.AccountName,
                    Date = l.Date,
                    JournalNumber = l.JournalNumber,
                    Description = l.Description,
                    Debit = l.Debit,
                    Credit = l.Credit
                };

                if (runningNet >= 0)
                {
                    row.BalanceDebit = runningNet;
                    row.BalanceCredit = 0;
                }
                else
                {
                    row.BalanceDebit = 0;
                    row.BalanceCredit = Math.Abs(runningNet);
                }

                result.Add(row);
            }
        }

        return result;
    }

    #endregion

    #region Profit & Loss

    public async Task<ProfitAndLossReportDto> GetProfitAndLossAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        var baseLines = db.JournalLines
            .Include(l => l.Account)
            .Include(l => l.JournalVoucher)
            .Where(l => l.JournalVoucher != null &&
                        l.JournalVoucher.Status == DocumentStatus.Posted &&
                        l.JournalVoucher.Date >= fromDate &&
                        l.JournalVoucher.Date <= toDate &&
                        (l.Account!.Type == AccountType.Revenue ||
                         l.Account!.Type == AccountType.Expense));

        if (branchId.HasValue)
            baseLines = baseLines.Where(l => l.JournalVoucher!.BranchId == branchId.Value);

        var grouped = await baseLines
            .GroupBy(l => new
            {
                l.AccountId,
                l.Account!.Code,
                l.Account!.Name,
                l.Account!.Type
            })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.Code,
                g.Key.Name,
                g.Key.Type,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            })
            .ToListAsync(cancellationToken);

        var lines = new List<ProfitAndLossLineDto>();
        decimal totalRevenue = 0;
        decimal totalExpense = 0;

        foreach (var g in grouped)
        {
            decimal amount;

            if (g.Type == AccountType.Expense)
            {
                // هزینه‌ها: مانده بدهکار مثبت
                amount = g.Debit - g.Credit;
                if (amount < 0) amount = 0;
                totalExpense += amount;
            }
            else // Revenue
            {
                // درآمدها: مانده بستانکار مثبت
                amount = g.Credit - g.Debit;
                if (amount < 0) amount = 0;
                totalRevenue += amount;
            }

            lines.Add(new ProfitAndLossLineDto
            {
                AccountId = g.AccountId,
                AccountCode = g.Code,
                AccountName = g.Name,
                AccountType = g.Type,
                Amount = amount
            });
        }

        var report = new ProfitAndLossReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalRevenue = totalRevenue,
            TotalExpense = totalExpense,
            NetProfitOrLoss = totalRevenue - totalExpense,
            Lines = lines
        };

        return report;
    }

    #endregion

    #region Balance Sheet

    public async Task<BalanceSheetReportDto> GetBalanceSheetAsync(
        DateTime asOfDate,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        var baseLines = db.JournalLines
            .Include(l => l.Account)
            .Include(l => l.JournalVoucher)
            .Where(l => l.JournalVoucher != null &&
                        l.JournalVoucher.Status == DocumentStatus.Posted &&
                        l.JournalVoucher.Date <= asOfDate);

        if (branchId.HasValue)
            baseLines = baseLines.Where(l => l.JournalVoucher!.BranchId == branchId.Value);

        // فقط حساب‌های ترازنامه‌ای (دارایی، بدهی، حقوق صاحبان سهام)
        var grouped = await baseLines
            .Where(l =>
                l.Account!.Type == AccountType.Asset ||
                l.Account!.Type == AccountType.Liability ||
                l.Account!.Type == AccountType.Equity)
            .GroupBy(l => new
            {
                l.AccountId,
                l.Account!.Code,
                l.Account!.Name,
                l.Account!.Type
            })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.Code,
                g.Key.Name,
                g.Key.Type,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            })
            .ToListAsync(cancellationToken);

        var lines = new List<BalanceSheetLineDto>();

        decimal totalAssets = 0;
        decimal totalLiabilities = 0;
        decimal totalEquity = 0;

        foreach (var g in grouped)
        {
            var net = g.Debit - g.Credit; // مانده = بدهکار - بستانکار

            decimal debit = 0;
            decimal credit = 0;

            if (net >= 0)
                debit = net;
            else
                credit = Math.Abs(net);

            lines.Add(new BalanceSheetLineDto
            {
                AccountId = g.AccountId,
                AccountCode = g.Code,
                AccountName = g.Name,
                AccountType = g.Type,
                Debit = debit,
                Credit = credit
            });

            decimal amount;
            switch (g.Type)
            {
                case AccountType.Asset:
                    // دارایی‌ها طبیعی بدهکار
                    amount = debit - credit;
                    totalAssets += amount;
                    break;

                case AccountType.Liability:
                    // بدهی‌ها طبیعی بستانکار
                    amount = credit - debit;
                    totalLiabilities += amount;
                    break;

                case AccountType.Equity:
                    // حقوق صاحبان سهام هم طبیعی بستانکار
                    amount = credit - debit;
                    totalEquity += amount;
                    break;
            }
        }

        return new BalanceSheetReportDto
        {
            AsOfDate = asOfDate,
            Lines = lines,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalEquity = totalEquity
        };
    }

    #endregion
}