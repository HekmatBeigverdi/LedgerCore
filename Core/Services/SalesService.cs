using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class SalesService(IUnitOfWork uow) : ISalesService
{
    #region Public API

    public async Task<SalesInvoice> CreateSalesInvoiceAsync(
        SalesInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);

        try
        {
            await ValidateCustomerAsync(invoice.CustomerId, cancellationToken);
            await ValidateWarehouseAsync(invoice.WarehouseId, cancellationToken);

            await CalculateInvoiceLinesAndTotalsAsync(invoice, cancellationToken);

            invoice.Number = await GenerateNextNumberAsync("SalesInvoice", invoice.BranchId, cancellationToken);
            invoice.Status = DocumentStatus.Draft;

            await uow.Invoices.AddSalesInvoiceAsync(invoice, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);

            return invoice;
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public Task<SalesInvoice?> GetSalesInvoiceAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return uow.Invoices.GetSalesInvoiceWithLinesAsync(id, cancellationToken);
    }

    public async Task<SalesInvoice> UpdateSalesInvoiceAsync(
        SalesInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        var existing = await uow.Invoices.GetSalesInvoiceWithLinesAsync(invoice.Id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException($"SalesInvoice with id={invoice.Id} not found.");

        if (existing.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("Posted invoice cannot be updated.");

        // فیلدهای ساده هدر را آپدیت می‌کنیم
        existing.Date = invoice.Date;
        existing.DueDate = invoice.DueDate;
        existing.CustomerId = invoice.CustomerId;
        existing.BranchId = invoice.BranchId;
        existing.WarehouseId = invoice.WarehouseId;
        existing.CurrencyId = invoice.CurrencyId;
        existing.FxRate = invoice.FxRate;
        existing.IsCashSale = invoice.IsCashSale;

        // سطرها را به‌صورت ساده ری‌بیلد می‌کنیم (برای شروع)
        existing.Lines.Clear();
        foreach (var line in invoice.Lines)
        {
            existing.Lines.Add(new InvoiceLine
            {
                LineNumber = line.LineNumber,
                Description = line.Description,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Discount = line.Discount,
                TaxRateId = line.TaxRateId
            });
        }

        await CalculateInvoiceLinesAndTotalsAsync(existing, cancellationToken);

        uow.Invoices.UpdateSalesInvoice(existing);
        await uow.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task PostSalesInvoiceAsync(
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);

        try
        {
            var invoice = await uow.Invoices.GetSalesInvoiceWithLinesAsync(invoiceId, cancellationToken);
            if (invoice is null)
                throw new InvalidOperationException($"SalesInvoice with id={invoiceId} not found.");

            if (invoice.Status == DocumentStatus.Posted)
                return; // قبلاً پست شده

            // 1) به‌روزرسانی موجودی و ثبت StockMove
            await PostToInventoryAsync(invoice, cancellationToken);

            // 2) ساخت سند حسابداری و سطرهای آن
            var journal = await PostToAccountingAsync(invoice, cancellationToken);

            invoice.Status = DocumentStatus.Posted;
            invoice.JournalVoucherId = journal.Id;

            uow.Invoices.UpdateSalesInvoice(invoice);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    #endregion

    #region Private helpers
    
    private async Task<int> GetOpenFiscalPeriodIdAsync(DateTime date, CancellationToken ct)
    {
        var fyRepo = uow.Repository<FiscalYear>();
        var fyPage = await fyRepo.FindAsync(y => y.StartDate <= date && y.EndDate >= date, null, ct);
        var year = fyPage.Items.OrderByDescending(y => y.StartDate).FirstOrDefault()
                   ?? throw new InvalidOperationException($"No fiscal year found for date={date:yyyy-MM-dd}.");

        if (year.IsClosed)
            throw new InvalidOperationException($"Fiscal year '{year.Name}' is closed.");

        var fpRepo = uow.Repository<FiscalPeriod>();
        var fpPage = await fpRepo.FindAsync(p => p.FiscalYearId == year.Id && p.StartDate <= date && p.EndDate >= date, null, ct);
        var period = fpPage.Items.OrderByDescending(p => p.StartDate).FirstOrDefault()
                     ?? throw new InvalidOperationException($"No fiscal period found for date={date:yyyy-MM-dd}.");

        if (period.IsClosed)
            throw new InvalidOperationException($"Fiscal period '{period.Name}' is closed.");

        return period.Id;
    }


    private async Task ValidateCustomerAsync(int customerId, CancellationToken cancellationToken)
    {
        var customer = await uow.Parties.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
            throw new InvalidOperationException($"Customer with id={customerId} not found.");

        if (!customer.IsActive)
            throw new InvalidOperationException("Customer is not active.");
    }

    private async Task ValidateWarehouseAsync(int? warehouseId, CancellationToken cancellationToken)
    {
        if (warehouseId is null)
            throw new InvalidOperationException("WarehouseId is required.");

        var warehouse = await uow.Warehouses.GetByIdAsync(warehouseId.Value, cancellationToken);
        if (warehouse is null)
            throw new InvalidOperationException($"Warehouse with id={warehouseId} not found.");
        if (!warehouse.IsActive)
            throw new InvalidOperationException("Warehouse is not active.");
    }

    /// <summary>
    /// محاسبه NetAmount, TaxAmount, TotalAmount سطرها و جمع کل فاکتور.
    /// </summary>
    private async Task CalculateInvoiceLinesAndTotalsAsync(
        SalesInvoice invoice,
        CancellationToken cancellationToken)
    {
        decimal totalNet = 0m;
        decimal totalDiscount = 0m;
        decimal totalTax = 0m;

        foreach (var line in invoice.Lines)
        {
            var product = await uow.Products.GetByIdAsync(line.ProductId, cancellationToken)
                          ?? throw new InvalidOperationException($"Product with id={line.ProductId} not found.");

            // اگر TaxRateId خالی است، از DefaultTaxRate محصول استفاده می‌کنیم
            if (line.TaxRateId is null && product.DefaultTaxRateId is not null)
            {
                line.TaxRateId = product.DefaultTaxRateId;
            }

            decimal taxPercent = 0m;
            if (line.TaxRateId is not null)
            {
                var taxRate = await uow.TaxRates.GetByIdAsync(line.TaxRateId.Value, cancellationToken)
                              ?? throw new InvalidOperationException($"TaxRate with id={line.TaxRateId} not found.");
                taxPercent = taxRate.RatePercent;
            }

            var gross = line.Quantity * line.UnitPrice;
            line.NetAmount = gross - line.Discount;
            line.TaxAmount = Math.Round(line.NetAmount * taxPercent / 100m, 2);
            line.TotalAmount = line.NetAmount + line.TaxAmount;

            totalNet += line.NetAmount;
            totalDiscount += line.Discount;
            totalTax += line.TaxAmount;
        }

        invoice.TotalNetAmount = totalNet;
        invoice.TotalDiscount = totalDiscount;
        invoice.TotalTaxAmount = totalTax;
        invoice.TotalAmount = totalNet + totalTax;
    }

    /// <summary>
    /// برداشت موجودی از انبار و ثبت حرکت StockMove.
    /// </summary>
    private async Task PostToInventoryAsync(
        SalesInvoice invoice,
        CancellationToken cancellationToken)
    {
        if (invoice.WarehouseId is null)
            throw new InvalidOperationException("WarehouseId is required to post inventory.");

        int wid = invoice.WarehouseId.Value;

        foreach (var line in invoice.Lines)
        {
            var stockItem = await uow.Stock.GetStockItemAsync(wid, line.ProductId, cancellationToken);

            if (stockItem is null)
            {
                // اگر تا حالا موجودی این کالا در این انبار ثبت نشده:
                stockItem = new StockItem
                {
                    WarehouseId = wid,
                    ProductId = line.ProductId,
                    OnHand = 0,
                    Reserved = 0,
                    AverageCost = 0
                };
                await uow.Stock.AddStockItemAsync(stockItem, cancellationToken);
            }

            if (stockItem.OnHand < line.Quantity)
            {
                // می‌تونی بر اساس سیاست شرکت اجازه منفی بدهی؛ فعلاً خطا می‌گیریم
                throw new InvalidOperationException(
                    $"Insufficient stock for productId={line.ProductId} in warehouseId={wid}.");
            }

            stockItem.OnHand -= line.Quantity;        // خروج از انبار
            uow.Stock.UpdateStockItem(stockItem);

            var move = new StockMove
            {
                Date = invoice.Date,
                WarehouseId = wid,
                ProductId = line.ProductId,
                MoveType = StockMoveType.Outbound,
                Quantity = line.Quantity,
                UnitCost = stockItem.AverageCost, // FIFO / متوسط وزنی مفصل‌ترش بعداً
                RefDocumentType = "SalesInvoice",
                RefDocumentId = invoice.Id,
                RefDocumentLineId = line.Id
            };

            await uow.Stock.AddStockMoveAsync(move, cancellationToken);
        }
    }

    /// <summary>
    /// ساخت سند حسابداری از روی فاکتور فروش بر اساس PostingRule.
    /// </summary>
    private async Task<JournalVoucher> PostToAccountingAsync(
        SalesInvoice invoice,
        CancellationToken cancellationToken)
    {
        // 1) پیدا کردن PostingRule مناسب برای SalesInvoice
        var postingRuleRepo = uow.Repository<PostingRule>();
        var postingRules = await postingRuleRepo.FindAsync(
            x => x.DocumentType == "SalesInvoice" && x.IsActive,
            null,
            cancellationToken);

        var postingRule = postingRules.Items.FirstOrDefault();
        if (postingRule is null)
            throw new InvalidOperationException("No active posting rule found for SalesInvoice.");
        
        var fiscalPeriodId = await GetOpenFiscalPeriodIdAsync(invoice.Date, cancellationToken);


        // 2) ساخت JV
        var voucher = new JournalVoucher
        {
            Number = await GenerateNextNumberAsync("Journal", invoice.BranchId, cancellationToken),
            Date = invoice.Date,
            BranchId = invoice.BranchId,
            FiscalPeriodId = fiscalPeriodId,
            Description = $"Posting Sales Invoice {invoice.Number}",
            Status = DocumentStatus.Draft
        };


        var lines = new List<JournalLine>();
        int lineNo = 1;

        // Debit: حساب مشتری / حساب نقدی (بسته به IsCashSale)
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = postingRule.DebitAccountId,
            Debit = invoice.TotalAmount,
            Credit = 0,
            RefDocumentType = "SalesInvoice",
            RefDocumentId = invoice.Id,
            CurrencyId = invoice.CurrencyId,
            FxRate = invoice.FxRate,
            Description = $"Receivable/Cash for invoice {invoice.Number}"
        });

        // Credit: Sales Revenue
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = postingRule.CreditAccountId,
            Debit = 0,
            Credit = invoice.TotalNetAmount,
            RefDocumentType = "SalesInvoice",
            RefDocumentId = invoice.Id,
            CurrencyId = invoice.CurrencyId,
            FxRate = invoice.FxRate,
            Description = $"Sales revenue for invoice {invoice.Number}"
        });

        // Credit Tax
        if (postingRule.TaxAccountId.HasValue && invoice.TotalTaxAmount > 0)
        {
            lines.Add(new JournalLine
            {
                LineNumber = lineNo++,
                AccountId = postingRule.TaxAccountId.Value,
                Debit = 0,
                Credit = invoice.TotalTaxAmount,
                RefDocumentType = "SalesInvoice",
                RefDocumentId = invoice.Id,
                CurrencyId = invoice.CurrencyId,
                FxRate = invoice.FxRate,
                Description = $"Tax for invoice {invoice.Number}"
            });
        }

        voucher.Lines = lines;

        await uow.Journals.AddAsync(voucher, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return voucher;
    }

    /// <summary>
    /// گرفتن شماره بعدی از NumberSeries برای یک EntityType خاص.
    /// </summary>
    private async Task<string> GenerateNextNumberAsync(
        string entityType,
        int? branchId,
        CancellationToken cancellationToken)
    {
        var seriesRepo = uow.Repository<NumberSeries>();

        var seriesPage = await seriesRepo.FindAsync(
            x => x.EntityType == entityType
                 && x.IsActive
                 && (x.BranchId == null || x.BranchId == branchId),
            null,
            cancellationToken);

        var series = seriesPage.Items
            .OrderByDescending(x => x.BranchId.HasValue) // اول سریال مخصوص شعبه
            .FirstOrDefault();

        if (series is null)
            throw new InvalidOperationException($"No NumberSeries defined for entityType={entityType}.");

        series.CurrentNumber += 1;
        seriesRepo.Update(series);

        await uow.SaveChangesAsync(cancellationToken);

        var number = series.CurrentNumber.ToString().PadLeft(series.Padding, '0');
        return $"{series.Prefix}{number}{series.Suffix}";
    }

    #endregion
}
