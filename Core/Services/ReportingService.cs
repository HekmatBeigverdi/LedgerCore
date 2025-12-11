using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.ViewModels.Dashboard;
using LedgerCore.Core.ViewModels.Reports;
using LedgerCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Core.Services;

public class ReportingService(LedgerCoreDbContext db) : IReportingService
{
    private readonly LedgerCoreDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

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

        var openingQuery = baseLines.Where(l => l.JournalVoucher!.Date < fromDate);

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

        foreach (var o in opening)
        {
            var row = GetOrCreate(o.AccountId, o.Code, o.Name);
            var net = o.Debit - o.Credit;

            if (net > 0)
                row.OpeningDebit = net;
            else if (net < 0)
                row.OpeningCredit = Math.Abs(net);
        }

        foreach (var p in period)
        {
            var row = GetOrCreate(p.AccountId, p.Code, p.Name);
            var net = p.Debit - p.Credit;

            if (net > 0)
                row.PeriodDebit = net;
            else if (net < 0)
                row.PeriodCredit = Math.Abs(net);
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
            x => x.Debit - x.Credit);

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
                amount = g.Debit - g.Credit;
                if (amount < 0) amount = 0;
                totalExpense += amount;
            }
            else // Revenue
            {
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

        return new ProfitAndLossReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalRevenue = totalRevenue,
            TotalExpense = totalExpense,
            NetProfitOrLoss = totalRevenue - totalExpense,
            Lines = lines
        };
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
            var net = g.Debit - g.Credit;

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
                    amount = debit - credit;
                    totalAssets += amount;
                    break;

                case AccountType.Liability:
                    amount = credit - debit;
                    totalLiabilities += amount;
                    break;

                case AccountType.Equity:
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

        if (branchId.HasValue)
            moves = moves.Where(m => m.Warehouse!.BranchId == branchId.Value);

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
                OutQty = g.Where(x => x.MoveType == StockMoveType.Outbound).Sum(x => x.Quantity),
                AdjQty = g.Where(x => x.MoveType == StockMoveType.Adjustment).Sum(x => x.Quantity)
            })
            .ToListAsync(cancellationToken);

        return grouped
            .Select(g => new StockBalanceRowDto
            {
                ProductId = g.ProductId,
                ProductCode = g.Code,
                ProductName = g.Name,
                WarehouseId = g.WarehouseId,
                WarehouseName = g.WarehouseName,
                QuantityOnHand = g.InQty - g.OutQty + g.AdjQty
            })
            .Where(x => x.QuantityOnHand != 0)
            .OrderBy(x => x.ProductCode)
            .ThenBy(x => x.WarehouseName)
            .ToList();
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
            .Where(m => m.ProductId == productId &&
                        m.Date >= fromDate &&
                        m.Date <= toDate);

        if (warehouseId.HasValue)
            moves = moves.Where(m => m.WarehouseId == warehouseId.Value);

        if (branchId.HasValue)
            moves = moves.Where(m => m.Warehouse!.BranchId == branchId.Value);

        var list = await moves
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Id)
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
            else if (m.MoveType == StockMoveType.Adjustment)
            {
                if (m.Quantity >= 0)
                    inQty = m.Quantity;
                else
                    outQty = Math.Abs(m.Quantity);
            }

            runningQty += inQty - outQty;

            decimal? unitCost = m.UnitCost;
            decimal? totalAmount = unitCost.HasValue ? unitCost.Value * m.Quantity : null;

            result.Add(new StockCardRowDto
            {
                Date = m.Date,
                DocumentType = m.RefDocumentType ?? string.Empty,
                DocumentNumber = m.RefDocumentId.HasValue ? m.RefDocumentId.Value.ToString() : string.Empty,
                Description = null,
                WarehouseId = m.WarehouseId,
                WarehouseName = m.Warehouse?.Name,
                InQuantity = inQty,
                OutQuantity = outQty,
                BalanceQuantity = runningQty,
                UnitCost = unitCost,
                TotalAmount = totalAmount
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
                Amount = g.Sum(x => x.TotalAmount)
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
    
    #region Sales by Party
    public async Task<IReadOnlyList<SalesByPartyRowDto>> GetSalesByPartyAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        var invoices = _db.SalesInvoices
            .Include(i => i.Customer)
            .Where(i => i.Date >= fromDate &&
                        i.Date <= toDate &&
                        i.Status == DocumentStatus.Posted);

        if (branchId.HasValue)
            invoices = invoices.Where(i => i.BranchId == branchId.Value);

        var grouped = await invoices
            .GroupBy(i => new
            {
                i.CustomerId,
                i.Customer!.Code,
                i.Customer!.Name
            })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.Code,
                g.Key.Name,
                Amount = g.Sum(x => x.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        return grouped
            .Select(g => new SalesByPartyRowDto
            {
                PartyId = g.CustomerId,
                PartyCode = g.Code,
                PartyName = g.Name,
                TotalAmount = g.Amount
            })
            .OrderBy(x => x.PartyCode)
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
    
    #region Payroll Reports

    public async Task<IReadOnlyList<PayrollByEmployeeRowDto>> GetPayrollByEmployeeAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        int? costCenterId,
        CancellationToken cancellationToken = default)
    {
        // فقط اسناد حقوقی پست شده یا پرداخت شده
        var docsQuery = _db.PayrollDocuments
            .Where(d =>
                d.Date >= fromDate &&
                d.Date <= toDate &&
                (d.Status == PayrollStatus.Posted || d.Status == PayrollStatus.Paid));

        // فیلتر شعبه/مرکز هزینه روی Employee
        // برای کارایی، هم روی سند و هم روی سطر اعمال می‌کنیم
        if (branchId.HasValue || costCenterId.HasValue)
        {
            docsQuery = docsQuery.Where(d =>
                d.Lines.Any(l =>
                    (!branchId.HasValue || l.Employee!.BranchId == branchId.Value) &&
                    (!costCenterId.HasValue || l.Employee!.CostCenterId == costCenterId.Value)));
        }

        var grouped = await docsQuery
            .SelectMany(d => d.Lines.Select(l => new
            {
                Document = d,
                Line = l,
                Employee = l.Employee!
            }))
            .Where(x =>
                !branchId.HasValue || x.Employee.BranchId == branchId.Value)
            .Where(x =>
                !costCenterId.HasValue || x.Employee.CostCenterId == costCenterId.Value)
            .GroupBy(x => new
            {
                x.Employee.Id,
                x.Employee.PersonnelCode,
                x.Employee.FullName,
                x.Employee.BranchId,
                BranchCode = x.Employee.Branch != null ? x.Employee.Branch.Code : null,
                BranchName = x.Employee.Branch != null ? x.Employee.Branch.Name : null,
                x.Employee.CostCenterId,
                CostCenterCode = x.Employee.CostCenter != null ? x.Employee.CostCenter.Code : null,
                CostCenterName = x.Employee.CostCenter != null ? x.Employee.CostCenter.Name : null,
            })
            .Select(g => new PayrollByEmployeeRowDto
            {
                EmployeeId = g.Key.Id,
                PersonnelCode = g.Key.PersonnelCode,
                FullName = g.Key.FullName,
                BranchId = g.Key.BranchId,
                BranchCode = g.Key.BranchCode,
                BranchName = g.Key.BranchName,
                CostCenterId = g.Key.CostCenterId,
                CostCenterCode = g.Key.CostCenterCode,
                CostCenterName = g.Key.CostCenterName,
                TotalGross = g.Sum(x => x.Line.GrossAmount),
                TotalDeductions = g.Sum(x => x.Line.Deductions),
                TotalNet = g.Sum(x => x.Line.NetAmount),
                PayrollDocumentCount = g
                    .Select(x => x.Document.Id)
                    .Distinct()
                    .Count()
            })
            .OrderBy(r => r.PersonnelCode)
            .ThenBy(r => r.FullName)
            .ToListAsync(cancellationToken);

        return grouped;
    }

    public async Task<IReadOnlyList<PayrollSummaryRowDto>> GetPayrollSummaryByBranchAndCostCenterAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        int? costCenterId,
        CancellationToken cancellationToken = default)
    {
        var docsQuery = _db.PayrollDocuments
            .Where(d =>
                d.Date >= fromDate &&
                d.Date <= toDate &&
                (d.Status == PayrollStatus.Posted || d.Status == PayrollStatus.Paid));

        if (branchId.HasValue || costCenterId.HasValue)
        {
            docsQuery = docsQuery.Where(d =>
                d.Lines.Any(l =>
                    (!branchId.HasValue || l.Employee!.BranchId == branchId.Value) &&
                    (!costCenterId.HasValue || l.Employee!.CostCenterId == costCenterId.Value)));
        }

        var grouped = await docsQuery
            .SelectMany(d => d.Lines.Select(l => new
            {
                Document = d,
                Line = l,
                Employee = l.Employee!
            }))
            .Where(x =>
                !branchId.HasValue || x.Employee.BranchId == branchId.Value)
            .Where(x =>
                !costCenterId.HasValue || x.Employee.CostCenterId == costCenterId.Value)
            .GroupBy(x => new
            {
                x.Employee.BranchId,
                BranchCode = x.Employee.Branch != null ? x.Employee.Branch.Code : null,
                BranchName = x.Employee.Branch != null ? x.Employee.Branch.Name : null,
                x.Employee.CostCenterId,
                CostCenterCode = x.Employee.CostCenter != null ? x.Employee.CostCenter.Code : null,
                CostCenterName = x.Employee.CostCenter != null ? x.Employee.CostCenter.Name : null,
            })
            .Select(g => new PayrollSummaryRowDto
            {
                BranchId = g.Key.BranchId,
                BranchCode = g.Key.BranchCode,
                BranchName = g.Key.BranchName,
                CostCenterId = g.Key.CostCenterId,
                CostCenterCode = g.Key.CostCenterCode,
                CostCenterName = g.Key.CostCenterName,
                TotalGross = g.Sum(x => x.Line.GrossAmount),
                TotalDeductions = g.Sum(x => x.Line.Deductions),
                TotalNet = g.Sum(x => x.Line.NetAmount),
                EmployeeCount = g
                    .Select(x => x.Employee.Id)
                    .Distinct()
                    .Count(),
                PayrollDocumentCount = g
                    .Select(x => x.Document.Id)
                    .Distinct()
                    .Count()
            })
            .OrderBy(r => r.BranchCode)
            .ThenBy(r => r.CostCenterCode)
            .ToListAsync(cancellationToken);

        return grouped;
    }

    #endregion
    
    #region Dashboard Summary

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        // مبنا: امروز و ماه جاری
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        // ====== Base Queries (فقط اسناد Post شده) ======
        var salesQuery = _db.SalesInvoices
            .AsNoTracking()
            .Where(x => x.Status == DocumentStatus.Posted);

        var purchasesQuery = _db.PurchaseInvoices
            .AsNoTracking()
            .Where(x => x.Status == DocumentStatus.Posted);

        var receiptsQuery = _db.Receipts
            .AsNoTracking()
            .Where(x => x.Status == DocumentStatus.Posted);

        var paymentsQuery = _db.Payments
            .AsNoTracking()
            .Where(x => x.Status == DocumentStatus.Posted);

        if (branchId.HasValue)
        {
            salesQuery = salesQuery.Where(x => x.BranchId == branchId.Value);
            purchasesQuery = purchasesQuery.Where(x => x.BranchId == branchId.Value);
            receiptsQuery = receiptsQuery.Where(x => x.BranchId == branchId.Value);
            paymentsQuery = paymentsQuery.Where(x => x.BranchId == branchId.Value);
        }

        // ===== فروش امروز =====
        var todaySalesAgg = await salesQuery
            .Where(x => x.Date >= today && x.Date < today.AddDays(1))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Total = g.Sum(x => x.TotalAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var monthSalesAgg = await salesQuery
            .Where(x => x.Date >= monthStart && x.Date < nextMonthStart)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Total = g.Sum(x => x.TotalAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // ===== خرید امروز / ماه =====
        var todayPurchasesAgg = await purchasesQuery
            .Where(x => x.Date >= today && x.Date < today.AddDays(1))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Total = g.Sum(x => x.TotalAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var monthPurchasesAgg = await purchasesQuery
            .Where(x => x.Date >= monthStart && x.Date < nextMonthStart)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Total = g.Sum(x => x.TotalAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // ===== دریافت‌ها / پرداخت‌های نقدی =====

        var todayReceiptsTotal = await receiptsQuery
            .Where(x => x.Date >= today && x.Date < today.AddDays(1))
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var monthReceiptsTotal = await receiptsQuery
            .Where(x => x.Date >= monthStart && x.Date < nextMonthStart)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var todayPaymentsTotal = await paymentsQuery
            .Where(x => x.Date >= today && x.Date < today.AddDays(1))
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var monthPaymentsTotal = await paymentsQuery
            .Where(x => x.Date >= monthStart && x.Date < nextMonthStart)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        // ===== تعداد مشتری / تأمین‌کننده =====
        var customerCount = await _db.Parties
            .AsNoTracking()
            .CountAsync(p =>
                p.Type == PartyType.Customer ||
                p.Type == PartyType.Both,
                cancellationToken);

        var supplierCount = await _db.Parties
            .AsNoTracking()
            .CountAsync(p =>
                p.Type == PartyType.Supplier ||
                p.Type == PartyType.Both,
                cancellationToken);

        // ===== فاکتورهای فروش معوق (براساس DueDate) =====
        var overdueSalesAgg = await salesQuery
            .Where(x => x.DueDate.HasValue && x.DueDate.Value < today)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Total = g.Sum(x => x.TotalAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // ===== ساخت DTO خروجی =====
        var result = new DashboardSummaryDto
        {
            Today = today,
            MonthStart = monthStart,
            MonthEnd = nextMonthStart.AddDays(-1),
            BranchId = branchId,

            TodaySalesInvoiceCount = todaySalesAgg?.Count ?? 0,
            TodaySalesTotal = todaySalesAgg?.Total ?? 0m,
            ThisMonthSalesInvoiceCount = monthSalesAgg?.Count ?? 0,
            ThisMonthSalesTotal = monthSalesAgg?.Total ?? 0m,

            TodayPurchaseInvoiceCount = todayPurchasesAgg?.Count ?? 0,
            TodayPurchaseTotal = todayPurchasesAgg?.Total ?? 0m,
            ThisMonthPurchaseInvoiceCount = monthPurchasesAgg?.Count ?? 0,
            ThisMonthPurchaseTotal = monthPurchasesAgg?.Total ?? 0m,

            TodayReceiptsTotal = todayReceiptsTotal,
            TodayPaymentsTotal = todayPaymentsTotal,
            ThisMonthReceiptsTotal = monthReceiptsTotal,
            ThisMonthPaymentsTotal = monthPaymentsTotal,

            CustomerCount = customerCount,
            SupplierCount = supplierCount,

            OverdueSalesInvoiceCount = overdueSalesAgg?.Count ?? 0,
            OverdueSalesInvoiceAmount = overdueSalesAgg?.Total ?? 0m
        };

        return result;
    }

    #endregion
    
    #region Daily Sales Trend

    public async Task<IReadOnlyList<DailySalesTrendDto>> GetDailySalesTrendAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        fromDate = fromDate.Date;
        toDate = toDate.Date;

        if (toDate < fromDate)
        {
            throw new ArgumentException("toDate must be greater than or equal to fromDate");
        }

        // فقط فاکتورهای فروش ثبت‌شده
        var salesQuery = _db.SalesInvoices
            .AsNoTracking()
            .Where(x => x.Status == DocumentStatus.Posted);

        if (branchId.HasValue)
        {
            salesQuery = salesQuery.Where(x => x.BranchId == branchId.Value);
        }

        // جمع فروش به تفکیک روز (در دیتابیس)
        var aggregated = await salesQuery
            .Where(x => x.Date >= fromDate && x.Date <= toDate)
            .GroupBy(x => x.Date.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count(),
                Total = g.Sum(x => x.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        // تبدیل لیست به دیکشنری برای پر کردن روزهای بدون فروش
        var dict = aggregated.ToDictionary(
            x => x.Date,
            x => new { x.Count, x.Total });

        var result = new List<DailySalesTrendDto>();

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            if (dict.TryGetValue(date, out var value))
            {
                result.Add(new DailySalesTrendDto
                {
                    Date = date,
                    SalesInvoiceCount = value.Count,
                    SalesTotal = value.Total
                });
            }
            else
            {
                // روزی که فروش ندارد، ولی برای نمودار صفر برمی‌گردانیم
                result.Add(new DailySalesTrendDto
                {
                    Date = date,
                    SalesInvoiceCount = 0,
                    SalesTotal = 0m
                });
            }
        }

        return result;
    }

    #endregion
    
    #region Branch Dashboard Summary

    public async Task<IReadOnlyList<BranchDashboardSummaryRowDto>> GetBranchDashboardSummaryAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        fromDate = fromDate.Date;
        toDate = toDate.Date;

        if (toDate < fromDate)
        {
            throw new ArgumentException("toDate must be greater than or equal to fromDate");
        }

        // امروز برای تشخیص overdue
        var today = DateTime.Today;

        // لیست شعب
        var branches = await _db.Branches
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken);

        // Base queries برای اسناد Post شده
        var salesQuery = _db.SalesInvoices
            .AsNoTracking()
            .Where(x => x.Status == DocumentStatus.Posted);

        var purchasesQuery = _db.PurchaseInvoices
            .AsNoTracking()
            .Where(x => x.Status == DocumentStatus.Posted);

        var receiptsQuery = _db.Receipts
            .AsNoTracking()
            .Where(x => x.Status == DocumentStatus.Posted);

        var paymentsQuery = _db.Payments
            .AsNoTracking()
            .Where(x => x.Status == DocumentStatus.Posted);

        // ===== فروش در بازهٔ زمانی به تفکیک شعبه =====
        var salesAgg = await salesQuery
            .Where(x => x.Date >= fromDate && x.Date <= toDate)
            .GroupBy(x => x.BranchId)
            .Select(g => new
            {
                BranchId = g.Key,
                Total = g.Sum(x => x.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        var salesDict = salesAgg.ToDictionary(
            x => x.BranchId,
            x => x.Total);

        // ===== خرید =====
        var purchaseAgg = await purchasesQuery
            .Where(x => x.Date >= fromDate && x.Date <= toDate)
            .GroupBy(x => x.BranchId)
            .Select(g => new
            {
                BranchId = g.Key,
                Total = g.Sum(x => x.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        var purchaseDict = purchaseAgg.ToDictionary(
            x => x.BranchId,
            x => x.Total);

        // ===== دریافت‌ها =====
        var receiptsAgg = await receiptsQuery
            .Where(x => x.Date >= fromDate && x.Date <= toDate)
            .GroupBy(x => x.BranchId)
            .Select(g => new
            {
                BranchId = g.Key,
                Total = g.Sum(x => x.Amount)
            })
            .ToListAsync(cancellationToken);

        var receiptsDict = receiptsAgg.ToDictionary(
            x => x.BranchId,
            x => x.Total);

        // ===== پرداخت‌ها =====
        var paymentsAgg = await paymentsQuery
            .Where(x => x.Date >= fromDate && x.Date <= toDate)
            .GroupBy(x => x.BranchId)
            .Select(g => new
            {
                BranchId = g.Key,
                Total = g.Sum(x => x.Amount)
            })
            .ToListAsync(cancellationToken);

        var paymentsDict = paymentsAgg.ToDictionary(
            x => x.BranchId,
            x => x.Total);

        // ===== فاکتورهای فروش معوق (تا امروز) به تفکیک شعبه =====
        var overdueAgg = await salesQuery
            .Where(x => x.DueDate.HasValue && x.DueDate.Value < today)
            .GroupBy(x => x.BranchId)
            .Select(g => new
            {
                BranchId = g.Key,
                Count = g.Count(),
                Total = g.Sum(x => x.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        var overdueDict = overdueAgg.ToDictionary(
            x => x.BranchId,
            x => new { x.Count, x.Total });

        // ===== ساخت خروجی به ازای هر شعبه =====
        var result = new List<BranchDashboardSummaryRowDto>();

        foreach (var branch in branches)
        {
            salesDict.TryGetValue(branch.Id, out var salesTotal);
            purchaseDict.TryGetValue(branch.Id, out var purchaseTotal);
            receiptsDict.TryGetValue(branch.Id, out var receiptsTotal);
            paymentsDict.TryGetValue(branch.Id, out var paymentsTotal);

            overdueDict.TryGetValue(branch.Id, out var overdueInfo);

            result.Add(new BranchDashboardSummaryRowDto
            {
                BranchId = branch.Id,
                BranchName = branch.Name,
                FromDate = fromDate,
                ToDate = toDate,

                SalesTotal = salesTotal,
                PurchaseTotal = purchaseTotal,
                ReceiptsTotal = receiptsTotal,
                PaymentsTotal = paymentsTotal,

                OverdueSalesInvoiceCount = overdueInfo?.Count ?? 0,
                OverdueSalesInvoiceAmount = overdueInfo?.Total ?? 0m
            });
        }

        return result;
    }

    #endregion
    
    #region Fiscal Status

    public async Task<IReadOnlyList<FiscalStatusRowDto>> GetFiscalStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var years = await _db.FiscalYears
            .AsNoTracking()
            .OrderBy(y => y.StartDate)
            .ToListAsync(cancellationToken);

        var yearIds = years.Select(y => y.Id).ToList();

        var periods = await _db.FiscalPeriods
            .AsNoTracking()
            .Where(p => yearIds.Contains(p.FiscalYearId))
            .OrderBy(p => p.StartDate)
            .ToListAsync(cancellationToken);

        var periodLookup = periods
            .GroupBy(p => p.FiscalYearId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<FiscalStatusRowDto>();

        foreach (var year in years)
        {
            if (!periodLookup.TryGetValue(year.Id, out var yearPeriods))
                yearPeriods = new List<FiscalPeriod>();

            foreach (var p in yearPeriods)
            {
                result.Add(new FiscalStatusRowDto
                {
                    FiscalYearId = year.Id,
                    FiscalYearName = year.Name,
                    FiscalYearStartDate = year.StartDate,
                    FiscalYearEndDate = year.EndDate,
                    FiscalYearIsClosed = year.IsClosed,
                    FiscalYearClosedAt = year.ClosedAt,

                    FiscalPeriodId = p.Id,
                    PeriodNumber = p.PeriodNumber,
                    FiscalPeriodName = p.Name,
                    FiscalPeriodStartDate = p.StartDate,
                    FiscalPeriodEndDate = p.EndDate,
                    FiscalPeriodIsClosed = p.IsClosed,
                    FiscalPeriodClosedAt = p.ClosedAt
                });
            }

            // اگر سالی هنوز دوره‌ای ندارد، یک ردیف فقط در سطح سال برمی‌گردانیم
            if (!yearPeriods.Any())
            {
                result.Add(new FiscalStatusRowDto
                {
                    FiscalYearId = year.Id,
                    FiscalYearName = year.Name,
                    FiscalYearStartDate = year.StartDate,
                    FiscalYearEndDate = year.EndDate,
                    FiscalYearIsClosed = year.IsClosed,
                    FiscalYearClosedAt = year.ClosedAt,

                    FiscalPeriodId = 0,
                    PeriodNumber = 0,
                    FiscalPeriodName = string.Empty,
                    FiscalPeriodStartDate = year.StartDate,
                    FiscalPeriodEndDate = year.EndDate,
                    FiscalPeriodIsClosed = year.IsClosed,
                    FiscalPeriodClosedAt = year.ClosedAt
                });
            }
        }

        return result;
    }

    #endregion

    
}