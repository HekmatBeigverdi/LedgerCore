using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.ViewModels.Reports;
using LedgerCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Core.Services;

public class ReportingService: IReportingService
{
    private readonly LedgerCoreDbContext _db;

    public ReportingService(LedgerCoreDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }
    #region Trial Balance

    public async Task<IReadOnlyList<TrialBalanceRow>> GetTrialBalanceAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        var baseLines = _db.JournalLines
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
        var baseLines = _db.JournalLines
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
        var baseLines = _db.JournalLines
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
        var baseLines = _db.JournalLines
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
    
    #region Stock Balance

    public async Task<IReadOnlyList<StockBalanceRowDto>> GetStockBalanceAsync(
        DateTime asOfDate,
        int? warehouseId,
        int? productId,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        var moves = _db.StockMoves
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .Where(m => m.Date <= asOfDate);

        if (warehouseId.HasValue)
            moves = moves.Where(m => m.WarehouseId == warehouseId.Value);

        if (productId.HasValue)
            moves = moves.Where(m => m.ProductId == productId.Value);
        

        // فرض: StockMoveType.In, StockMoveType.Out
        var grouped = await moves
            .GroupBy(m => new
            {
                m.ProductId,
                m.Product!.Code,
                m.Product!.Name,
                m.WarehouseId,
                WarehouseName = m.Warehouse != null ? m.Warehouse.Name : null
            })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Code,
                g.Key.Name,
                g.Key.WarehouseId,
                g.Key.WarehouseName,
                InQty = g.Where(x => x.MoveType == StockMoveType.Inbound).Sum(x => x.Quantity),
                OutQty = g.Where(x => x.MoveType == StockMoveType.Outbound).Sum(x => x.Quantity)
            })
            .ToListAsync(cancellationToken);

        var result = grouped
            .Select(g => new StockBalanceRowDto
            {
                ProductId = g.ProductId,
                ProductCode = g.Code,
                ProductName = g.Name,
                WarehouseId = g.WarehouseId,
                WarehouseName = g.WarehouseName,
                QuantityOnHand = g.InQty - g.OutQty
            })
            .Where(x => x.QuantityOnHand != 0)
            .OrderBy(x => x.ProductCode)
            .ThenBy(x => x.WarehouseName)
            .ToList();

        return result;
    }

    #endregion
    
    #region Stock Card

    public async Task<IReadOnlyList<StockCardRowDto>> GetStockCardAsync(
        int productId,
        int? warehouseId,
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        var moves = _db.StockMoves
            .Include(m => m.Warehouse)
            .Include(m => m.Product)
            .Where(m => m.ProductId == productId &&
                        m.Date >= fromDate &&
                        m.Date <= toDate);

        if (warehouseId.HasValue)
            moves = moves.Where(m => m.WarehouseId == warehouseId.Value);
        

        var list = await moves
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Id)
            .Select(m => new
            {
                m.Date,
                m.MoveType,
                m.Quantity,
                m.WarehouseId,
                WarehouseName = m.Warehouse != null ? m.Warehouse.Name : null,
                m.RefDocumentType,
            })
            .ToListAsync(cancellationToken);

        var result = new List<StockCardRowDto>();
        decimal runningQty = 0;

        foreach (var m in list)
        {
            decimal inQty = 0;
            decimal outQty = 0;

            if (m.MoveType == StockMoveType.Inbound)
                inQty = m.Quantity;
            else if (m.MoveType == StockMoveType.Outbound)
                outQty = m.Quantity;

            runningQty += inQty - outQty;

            result.Add(new StockCardRowDto
            {
                Date = m.Date,
                DocumentType = m.RefDocumentType ?? string.Empty,
                WarehouseId = m.WarehouseId,
                WarehouseName = m.WarehouseName,
                InQuantity = inQty,
                OutQuantity = outQty,
                BalanceQuantity = runningQty,
            });
        }

        return result;
    }

    #endregion
    
    #region Sales & Purchases

    public async Task<IReadOnlyList<SalesByItemRowDto>> GetSalesByItemAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        var invoices = _db.SalesInvoices
            .Include(i => i.Lines)
            .ThenInclude(l => l.Product)
            .Where(i => i.Date >= fromDate &&
                        i.Date <= toDate &&
                        i.Status == DocumentStatus.Posted);

        if (branchId.HasValue)
            invoices = invoices.Where(i => i.BranchId == branchId.Value);

        var grouped = await invoices
            .SelectMany(i => i.Lines)
            .GroupBy(l => new
            {
                l.ProductId,
                l.Product!.Code,
                l.Product!.Name,
            })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Code,
                g.Key.Name,
                Quantity = g.Sum(x => x.Quantity),
                Amount = g.Sum(x => x.TotalAmount) // اگر نام فیلد مبلغ متفاوت است، اینجا اصلاح کن
            })
            .ToListAsync(cancellationToken);

        return grouped
            .Select(g => new SalesByItemRowDto
            {
                ProductId = g.ProductId,
                ProductCode = g.Code,
                ProductName = g.Name,
                Quantity = g.Quantity,
                TotalAmount = g.Amount
            })
            .OrderBy(x => x.ProductCode)
            .ToList();
    }
    #endregion
    
    #region Purchases By Item

    public async Task<IReadOnlyList<PurchaseByItemRowDto>> GetPurchasesByItemAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        var invoices = _db.PurchaseInvoices
            .Include(i => i.Lines)
            .ThenInclude(l => l.Product)
            .Where(i => i.Date >= fromDate &&
                        i.Date <= toDate &&
                        i.Status == DocumentStatus.Posted);

        if (branchId.HasValue)
            invoices = invoices.Where(i => i.BranchId == branchId.Value);

        var grouped = await invoices
            .SelectMany(i => i.Lines)
            .GroupBy(l => new
            {
                l.ProductId,
                l.Product!.Code,
                l.Product!.Name
            })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Code,
                g.Key.Name,
                Quantity = g.Sum(x => x.Quantity),
                Amount = g.Sum(x => x.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        return grouped
            .Select(g => new PurchaseByItemRowDto
            {
                ProductId = g.ProductId,
                ProductCode = g.Code,
                ProductName = g.Name,
                Quantity = g.Quantity,
                TotalAmount = g.Amount
            })
            .OrderBy(x => x.ProductCode)
            .ToList();
    }
    #endregion
    
}