using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class SalesService(
    IUnitOfWork uow,
    ICurrentBranchService currentBranch,
    IAccountingService accountingService) : ISalesService
{
    #region Public API
    private int GetBranchIdOrThrow()
        => currentBranch.GetRequiredBranchId();

    private async Task<SalesInvoice?> GetSalesInvoiceScopedAsync(int id, CancellationToken ct)
    {
        var branchId = GetBranchIdOrThrow();

        // اگر Repository شما GetByIdAsync دارد:
        var inv = await uow.Invoices.GetSalesInvoiceWithLinesAsync(id, branchId, ct);
        return inv;
    }

    private async Task<SalesInvoice> GetSalesInvoiceScopedOrThrowAsync(int id, CancellationToken ct)
    {
        var inv = await GetSalesInvoiceScopedAsync(id, ct);
        if (inv is null)
            throw new InvalidOperationException($"Sales invoice with id={id} not found.");
        return inv;
    }

    public async Task<SalesInvoice> CreateSalesInvoiceAsync(
        SalesInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        
        if (invoice.BranchId == 0)
            invoice.BranchId = currentBranch.GetRequiredBranchId();


        try
        {
            await ValidateCustomerAsync(invoice.CustomerId, cancellationToken);
            await ValidateWarehouseAsync(invoice.WarehouseId, invoice.BranchId, cancellationToken);

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
        return GetSalesInvoiceScopedAsync(id, cancellationToken);
    }

    public async Task<SalesInvoice> UpdateSalesInvoiceAsync(
        SalesInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetSalesInvoiceScopedAsync(invoice.Id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException($"SalesInvoice with id={invoice.Id} not found.");

        if (existing.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("Posted invoice cannot be updated.");

        // فیلدهای ساده هدر را آپدیت می‌کنیم
        existing.Date = invoice.Date;
        existing.DueDate = invoice.DueDate;
        existing.CustomerId = invoice.CustomerId;
        if (invoice.BranchId != 0)        // اگر 0 بود، شعبه قبلی را نگه می‌داریم
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
        await ValidateWarehouseAsync(existing.WarehouseId, existing.BranchId, cancellationToken);


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
            var invoice = await GetSalesInvoiceScopedAsync(invoiceId, cancellationToken);
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
    public async Task VoidSalesInvoiceAsync(
        int invoiceId,
        DateTime? reversalDate = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var invoice = await GetSalesInvoiceScopedAsync(invoiceId, cancellationToken);
            if (invoice is null)
                throw new InvalidOperationException($"SalesInvoice with id={invoiceId} not found.");

            if (invoice.Status != DocumentStatus.Posted)
                throw new InvalidOperationException("Only a posted sales invoice can be voided.");

            if (invoice.JournalVoucherId is null)
                throw new InvalidOperationException("Posted sales invoice has no JournalVoucherId.");

            var revDate = (reversalDate ?? DateTime.UtcNow).Date;

            // 1) برگشت انبار: StockMove های مرجع SalesInvoice را پیدا کن
            var movesPage = await uow.Repository<StockMove>().FindAsync(
                m => m.RefDocumentType == "SalesInvoice" && m.RefDocumentId == invoice.Id,
                null,
                cancellationToken);

            var moves = movesPage.Items.ToList();
            if (moves.Count == 0)
                throw new InvalidOperationException("No stock moves found for this sales invoice.");

            foreach (var mv in moves)
            {
                var item = await uow.Stock.GetStockItemAsync(mv.WarehouseId, mv.ProductId, cancellationToken);
                if (item is null)
                    throw new InvalidOperationException($"StockItem not found (warehouseId={mv.WarehouseId}, productId={mv.ProductId}).");

                // فروش Outbound بوده، برای void باید برگردانیم (Inbound)
                item.OnHand += mv.Quantity;
                uow.Stock.UpdateStockItem(item);

                var revMove = new StockMove
                {
                    Date = revDate,
                    WarehouseId = mv.WarehouseId,
                    ProductId = mv.ProductId,
                    MoveType = StockMoveType.Inbound,
                    Quantity = mv.Quantity,
                    UnitCost = mv.UnitCost,
                    RefDocumentType = "SalesInvoiceVoid",
                    RefDocumentId = invoice.Id,
                    RefDocumentLineId = mv.RefDocumentLineId
                };

                await uow.Stock.AddStockMoveAsync(revMove, cancellationToken);
            }

            // 2) برگشت حسابداری: ژورنال معکوس بساز
            var reversalJournal = await ReverseJournalInternalAsync(
                invoice.JournalVoucherId.Value,
                revDate,
                description ?? $"Void SalesInvoice {invoice.Number} (id={invoice.Id})",
                cancellationToken);

            // 3) وضعیت فاکتور
            invoice.Status = DocumentStatus.Cancelled;
            invoice.ReversalJournalVoucherId = reversalJournal.Id;

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
    
    private async Task ValidateCustomerAsync(int customerId, CancellationToken cancellationToken)
    {
        var customer = await uow.Parties.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
            throw new InvalidOperationException($"Customer with id={customerId} not found.");

        if (!customer.IsActive)
            throw new InvalidOperationException("Customer is not active.");

        if (customer.Type != PartyType.Customer && customer.Type != PartyType.Both)
            throw new InvalidOperationException("Selected party is not a Customer.");
    }

    private async Task ValidateWarehouseAsync(int? warehouseId, int branchId, CancellationToken cancellationToken)
    {
        if (warehouseId is null)
            throw new InvalidOperationException("WarehouseId is required.");

        var warehouse = await uow.Warehouses.GetByIdAsync(warehouseId.Value, cancellationToken);

        if (warehouse is null)
            throw new InvalidOperationException($"Warehouse with id={warehouseId} not found.");

        if (warehouse.BranchId != branchId)
            throw new InvalidOperationException("Selected warehouse does not belong to invoice branch.");

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
        var postingRuleRepo = uow.Repository<PostingRule>();
        var postingRules = await postingRuleRepo.FindAsync(
            x => x.DocumentType == "SalesInvoice" && x.IsActive,
            null,
            cancellationToken);

        PostingRule? postingRule = null;

        if (invoice.IsCashSale)
            postingRule = postingRules.Items.FirstOrDefault(r => r.Code == "SalesInvoice_Cash");

        postingRule ??= postingRules.Items.FirstOrDefault(r => r.Code == "SalesInvoice_Credit");
        postingRule ??= postingRules.Items.FirstOrDefault();

        if (postingRule is null)
            throw new InvalidOperationException("No active posting rule found for SalesInvoice.");

        var lines = new List<JournalLine>();
        int lineNo = 1;

        // Debit: Receivable / Cash
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = postingRule.DebitAccountId,
            Debit = invoice.TotalAmount,
            Credit = 0m,
            PartyId = invoice.CustomerId,
            RefDocumentType = "SalesInvoice",
            RefDocumentId = invoice.Id,
            CurrencyId = invoice.CurrencyId,
            FxRate = invoice.FxRate,
            Description = $"Receivable/Cash for invoice {invoice.Number}"
        });

        // Credit: Revenue
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = postingRule.CreditAccountId,
            Debit = 0m,
            Credit = invoice.TotalNetAmount + invoice.TotalDiscount,
            RefDocumentType = "SalesInvoice",
            RefDocumentId = invoice.Id,
            CurrencyId = invoice.CurrencyId,
            FxRate = invoice.FxRate,
            Description = $"Sales revenue for invoice {invoice.Number}"
        });

        // Debit: Discount
        if (postingRule.DiscountAccountId.HasValue && invoice.TotalDiscount > 0)
        {
            lines.Add(new JournalLine
            {
                LineNumber = lineNo++,
                AccountId = postingRule.DiscountAccountId.Value,
                Debit = invoice.TotalDiscount,
                Credit = 0m,
                RefDocumentType = "SalesInvoice",
                RefDocumentId = invoice.Id,
                CurrencyId = invoice.CurrencyId,
                FxRate = invoice.FxRate,
                Description = $"Discount for invoice {invoice.Number}"
            });
        }

        // Credit: Tax
        if (postingRule.TaxAccountId.HasValue && invoice.TotalTaxAmount > 0)
        {
            lines.Add(new JournalLine
            {
                LineNumber = lineNo++,
                AccountId = postingRule.TaxAccountId.Value,
                Debit = 0m,
                Credit = invoice.TotalTaxAmount,
                RefDocumentType = "SalesInvoice",
                RefDocumentId = invoice.Id,
                CurrencyId = invoice.CurrencyId,
                FxRate = invoice.FxRate,
                Description = $"Tax for invoice {invoice.Number}"
            });
        }

        var voucher = new JournalVoucher
        {
            Number = "",
            Date = invoice.Date,
            BranchId = invoice.BranchId,
            FiscalPeriodId = null,
            Description = $"Posting Sales Invoice {invoice.Number}",
            Status = DocumentStatus.Draft,
            Lines = lines
        };

        var created = await accountingService.CreateJournalAsync(voucher, cancellationToken);
        await accountingService.PostJournalAsync(created.Id, cancellationToken);

        return await accountingService.GetJournalAsync(created.Id, cancellationToken) ?? created;
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
    private async Task<JournalVoucher> ReverseJournalInternalAsync(
        int journalId,
        DateTime reversalDate,
        string description,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        var original = await uow.Journals.GetWithLinesAsync(journalId, branchId, cancellationToken);
        if (original is null)
            throw new InvalidOperationException($"JournalVoucher with id={journalId} not found.");

        if (original.Status != DocumentStatus.Posted)
            throw new InvalidOperationException("Only a posted journal can be reversed.");

        var fiscalPeriodId = await GetOpenFiscalPeriodIdAsync(reversalDate, cancellationToken);

        var reversed = new JournalVoucher
        {
            Number = await GenerateNextNumberAsync("Journal", original.BranchId, cancellationToken),
            Date = reversalDate.Date,
            BranchId = original.BranchId,
            FiscalPeriodId = fiscalPeriodId,
            Description = description,
            Status = DocumentStatus.Posted,
            Lines = new List<JournalLine>()
        };

        var lineNo = 1;
        foreach (var l in original.Lines.OrderBy(x => x.LineNumber))
        {
            reversed.Lines.Add(new JournalLine
            {
                LineNumber = lineNo++,
                AccountId = l.AccountId,
                Debit = l.Credit,
                Credit = l.Debit,
                PartyId = l.PartyId,
                CostCenterId = l.CostCenterId,
                ProjectId = l.ProjectId,
                CurrencyId = l.CurrencyId,
                FxRate = l.FxRate,
                RefDocumentType = "JournalVoucher",
                RefDocumentId = original.Id,
                Description = $"Reversal of {original.Number}"
            });
        }

        await uow.Journals.AddAsync(reversed, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return reversed;
    }


    #endregion
}
