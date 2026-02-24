using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class PurchaseService(IUnitOfWork uow, ICurrentBranchService currentBranch) : IPurchaseService
{

    #region Public API
    
    

    public async Task<PurchaseInvoice> CreatePurchaseInvoiceAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        
        if (invoice.BranchId == 0)
            invoice.BranchId = currentBranch.GetRequiredBranchId();

        await uow.BeginTransactionAsync(cancellationToken);

        try
        {
            _ = await GetOpenFiscalPeriodIdAsync(invoice.Date, cancellationToken);
            ValidateInvoiceDates(invoice.Date, invoice.DueDate);
            
            await ValidateSupplierAsync(invoice.SupplierId, cancellationToken);
            await ValidateWarehouseIfSetAsync(invoice.WarehouseId, invoice.BranchId, cancellationToken);

            await CalculateInvoiceLinesAndTotalsAsync(invoice, cancellationToken);

            invoice.Number = await GenerateNextNumberAsync("PurchaseInvoice", invoice.BranchId, cancellationToken);
            invoice.Status = DocumentStatus.Draft;

            await uow.Invoices.AddPurchaseInvoiceAsync(invoice, cancellationToken);
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

    public Task<PurchaseInvoice?> GetPurchaseInvoiceAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var branchId = currentBranch.GetRequiredBranchId();
        return uow.Invoices.GetPurchaseInvoiceWithLinesAsync(id, branchId, cancellationToken);
    }

    public async Task<PurchaseInvoice> UpdatePurchaseInvoiceAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        var existing = await uow.Invoices.GetPurchaseInvoiceWithLinesAsync(invoice.Id, branchId, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException($"PurchaseInvoice with id={invoice.Id} not found.");

        if (existing.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("Posted purchase invoice cannot be updated.");
        
        _ = await GetOpenFiscalPeriodIdAsync(invoice.Date, cancellationToken);
        ValidateInvoiceDates(invoice.Date, invoice.DueDate);

        // هدر
        existing.Date = invoice.Date;
        existing.DueDate = invoice.DueDate;
        existing.SupplierId = invoice.SupplierId;
        if (invoice.BranchId != 0 && invoice.BranchId != existing.BranchId)
            throw new InvalidOperationException("Changing BranchId of an existing purchase invoice is not allowed.");
        existing.WarehouseId = invoice.WarehouseId;
        existing.CurrencyId = invoice.CurrencyId;
        existing.FxRate = invoice.FxRate;

        // سطرها را ری‌بیلد می‌کنیم (نسخه‌ی ساده)
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
        await ValidateWarehouseIfSetAsync(existing.WarehouseId, existing.BranchId, cancellationToken);

        uow.Invoices.UpdatePurchaseInvoice(existing);
        await uow.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task PostPurchaseInvoiceAsync(
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        await uow.BeginTransactionAsync(cancellationToken);

        try
        {
            var invoice = await uow.Invoices.GetPurchaseInvoiceWithLinesAsync(invoiceId, branchId, cancellationToken);
            if (invoice is null)
                throw new InvalidOperationException($"PurchaseInvoice with id={invoiceId} not found.");

            if (invoice.Status == DocumentStatus.Posted)
                return;
            
            _ = await GetOpenFiscalPeriodIdAsync(invoice.Date, cancellationToken);
            ValidateInvoiceDates(invoice.Date, invoice.DueDate);

            // 1) افزایش موجودی و ثبت StockMove
            await PostToInventoryAsync(invoice, cancellationToken);

            // 2) ثبت سند حسابداری
            var journal = await PostToAccountingAsync(invoice, cancellationToken);

            invoice.Status = DocumentStatus.Posted;
            invoice.JournalVoucherId = journal.Id;

            uow.Invoices.UpdatePurchaseInvoice(invoice);
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

    private async Task ValidateSupplierAsync(int supplierId, CancellationToken cancellationToken)
    {
        var supplier = await uow.Parties.GetByIdAsync(supplierId, cancellationToken);
        if (supplier is null)
            throw new InvalidOperationException($"Supplier with id={supplierId} not found.");

        if (!supplier.IsActive)
            throw new InvalidOperationException("Supplier is not active.");

        if (supplier.Type != PartyType.Supplier && supplier.Type != PartyType.Both)
            throw new InvalidOperationException("Selected party is not a Supplier.");
    }

    private async Task ValidateWarehouseIfSetAsync(int? warehouseId, int branchId, CancellationToken cancellationToken)
    {
        if (warehouseId is null)
            return;

        // اگر خواستی، IWarehouseRepository را هم روی IUnitOfWork اضافه کن
        var warehouseRepo = uow.Repository<Warehouse>();
        var warehouse = await warehouseRepo.GetByIdAsync(warehouseId.Value, cancellationToken);

        if (warehouse is null)
            throw new InvalidOperationException($"Warehouse with id={warehouseId} not found.");
        if (!warehouse.IsActive)
            throw new InvalidOperationException("Warehouse is not active.");
        if (warehouse.BranchId != branchId)
            throw new InvalidOperationException("Selected warehouse does not belong to invoice branch.");
        
    }
    private static void ValidateInvoiceDates(DateTime date, DateTime? dueDate)
    {
        if (date == default)
            throw new InvalidOperationException("Invoice date is required.");

        if (dueDate.HasValue)
        {
            if (dueDate.Value == default)
                throw new InvalidOperationException("Invoice due date is invalid.");

            if (dueDate.Value.Date < date.Date)
                throw new InvalidOperationException("Invoice due date cannot be earlier than invoice date.");
        }
    }
    private async Task ValidateAccountPartyRulesAsync(
        int accountId,
        int? partyId,
        PartyType? partyType,
        CancellationToken ct)
    {
        var accountRepo = uow.Repository<Account>();
        var account = await accountRepo.GetByIdAsync(accountId, ct);

        if (account is null)
            throw new InvalidOperationException($"Account with id={accountId} not found.");

        // RequiresParty
        if (account.RequiresParty)
        {
            if (!partyId.HasValue || partyId.Value <= 0)
                throw new InvalidOperationException(
                    $"Account '{account.Code} - {account.Name}' requires a party.");
        }

        // AllowedPartyType
        if (account.AllowedPartyType is not null)
        {
            if (!partyType.HasValue)
                throw new InvalidOperationException(
                    $"Account '{account.Code} - {account.Name}' requires party type validation.");

            if (account.AllowedPartyType != partyType.Value)
                throw new InvalidOperationException(
                    $"Account '{account.Code} - {account.Name}' does not allow party type '{partyType.Value}'.");
        }
    }

    private async Task CalculateInvoiceLinesAndTotalsAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken)
    {
        decimal totalNet = 0m;
        decimal totalDiscount = 0m;
        decimal totalTax = 0m;

        var taxRateRepo = uow.Repository<TaxRate>();
        var productRepo = uow.Products;

        foreach (var line in invoice.Lines)
        {
            var product = await productRepo.GetByIdAsync(line.ProductId, cancellationToken)
                          ?? throw new InvalidOperationException($"Product with id={line.ProductId} not found.");

            if (line.TaxRateId is null && product.DefaultTaxRateId is not null)
                line.TaxRateId = product.DefaultTaxRateId;

            decimal taxPercent = 0m;
            if (line.TaxRateId is not null)
            {
                var taxRate = await taxRateRepo.GetByIdAsync(line.TaxRateId.Value, cancellationToken)
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
    /// افزایش موجودی انبار و محاسبه‌ی متوسط قیمت تمام‌شده.
    /// </summary>
    private async Task PostToInventoryAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken)
    {
        if (invoice.WarehouseId is null)
            return; // اگر فاکتور کالا ندارد، می‌توانی این‌جا return کنی

        foreach (var line in invoice.Lines)
        {
            var stockItem = await uow.Stock.GetStockItemAsync(invoice.WarehouseId.Value, line.ProductId, cancellationToken);

            if (stockItem is null)
            {
                stockItem = new StockItem
                {
                    WarehouseId = invoice.WarehouseId.Value,
                    ProductId = line.ProductId,
                    OnHand = 0,
                    Reserved = 0,
                    AverageCost = 0
                };
                await uow.Stock.AddStockItemAsync(stockItem, cancellationToken);
            }

            // محاسبه متوسط قیمت جدید (روش ساده: میانگین وزنی متحرک)
            var oldQty = stockItem.OnHand;
            var oldCost = stockItem.AverageCost;
            var newQty = line.Quantity;

            var newCost = line.NetAmount / (line.Quantity == 0 ? 1 : line.Quantity);

            var totalQty = oldQty + newQty;
            if (totalQty > 0)
            {
                var totalValue = (oldQty * oldCost) + (newQty * newCost);
                stockItem.AverageCost = totalValue / totalQty;
            }

            stockItem.OnHand += newQty;
            uow.Stock.UpdateStockItem(stockItem);

            var move = new StockMove
            {
                Date = invoice.Date,
                WarehouseId = invoice.WarehouseId.Value,
                ProductId = line.ProductId,
                MoveType = StockMoveType.Inbound,
                Quantity = line.Quantity,
                UnitCost = stockItem.AverageCost,
                RefDocumentType = "PurchaseInvoice",
                RefDocumentId = invoice.Id,
                RefDocumentLineId = line.Id
            };

            await uow.Stock.AddStockMoveAsync(move, cancellationToken);
        }
    }

    private async Task<JournalVoucher> PostToAccountingAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken)
    {
        var postingRuleRepo = uow.Repository<PostingRule>();
        var postingRules = await postingRuleRepo.FindAsync(
            x => x.DocumentType == "PurchaseInvoice" && x.IsActive,
            null,
            cancellationToken);

        var postingRule = postingRules.Items.FirstOrDefault();
        if (postingRule is null)
            throw new InvalidOperationException("No active posting rule found for PurchaseInvoice.");

        var fiscalPeriodId = await GetOpenFiscalPeriodIdAsync(invoice.Date, cancellationToken);

        // Supplier validation
        await ValidateSupplierAsync(invoice.SupplierId, cancellationToken);

        // برای خرید، نوع طرف حساب به صورت قراردادی Supplier است
        var supplierPartyType = PartyType.Supplier;

        // Debit account (Inventory/Expense) — معمولاً نباید Party بخواهد
        await ValidateAccountPartyRulesAsync(
            postingRule.DebitAccountId,
            partyId: null,
            partyType: null,
            cancellationToken);

        // Tax account — اگر وجود دارد
        if (postingRule.TaxAccountId.HasValue && invoice.TotalTaxAmount > 0)
        {
            await ValidateAccountPartyRulesAsync(
                postingRule.TaxAccountId.Value,
                partyId: null,
                partyType: null,
                cancellationToken);
        }

        // Credit account (Payable) — باید Supplier داشته باشد
        await ValidateAccountPartyRulesAsync(
            postingRule.CreditAccountId,
            partyId: invoice.SupplierId,
            partyType: supplierPartyType,
            cancellationToken);
        
        var voucher = new JournalVoucher
        {
            Number = await GenerateNextNumberAsync("Journal", invoice.BranchId, cancellationToken),
            Date = invoice.Date,
            Description = $"Posting Purchase Invoice {invoice.Number}",
            BranchId = invoice.BranchId,
            FiscalPeriodId = fiscalPeriodId,
            Status = DocumentStatus.Posted
        };

        var lines = new List<JournalLine>();
        int lineNo = 1;

        // Debit: Inventory + Tax
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = postingRule.DebitAccountId, // Inventory
            Debit = invoice.TotalNetAmount,
            Credit = 0,
            RefDocumentType = "PurchaseInvoice",
            RefDocumentId = invoice.Id,
            CurrencyId = invoice.CurrencyId,
            FxRate = invoice.FxRate,
            Description = $"Inventory for purchase {invoice.Number}"
        });

        if (postingRule.TaxAccountId.HasValue && invoice.TotalTaxAmount > 0)
        {
            lines.Add(new JournalLine
            {
                LineNumber = lineNo++,
                AccountId = postingRule.TaxAccountId.Value,
                Debit = invoice.TotalTaxAmount,
                Credit = 0,
                RefDocumentType = "PurchaseInvoice",
                RefDocumentId = invoice.Id,
                CurrencyId = invoice.CurrencyId,
                FxRate = invoice.FxRate,
                Description = $"Tax for purchase {invoice.Number}"
            });
        }

        // Credit: Payable / Cash
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = postingRule.CreditAccountId,
            Debit = 0,
            Credit = invoice.TotalAmount,
            PartyId = invoice.SupplierId,
            RefDocumentType = "PurchaseInvoice",
            RefDocumentId = invoice.Id,
            CurrencyId = invoice.CurrencyId,
            FxRate = invoice.FxRate,
            Description = $"Payable/Cash for purchase {invoice.Number}"
        });

        voucher.Lines = lines;

        await uow.Journals.AddAsync(voucher, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return voucher;
    }

    private async Task<string> GenerateNextNumberAsync(
        string entityType,
        int? branchId,
        CancellationToken cancellationToken)
    {
        var seriesRepo = uow.Repository<NumberSeries>();

        var page = await seriesRepo.FindAsync(
            x => x.EntityType == entityType
                 && x.IsActive
                 && (x.BranchId == null || x.BranchId == branchId),
            null,
            cancellationToken);

        var series = page.Items
            .OrderByDescending(x => x.BranchId.HasValue)
            .FirstOrDefault();

        if (series is null)
            throw new InvalidOperationException($"No NumberSeries defined for entityType={entityType}.");

        series.CurrentNumber += 1;
        seriesRepo.Update(series);
        await uow.SaveChangesAsync(cancellationToken);

        var num = series.CurrentNumber.ToString().PadLeft(series.Padding, '0');
        return $"{series.Prefix}{num}{series.Suffix}";
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
    
    #endregion
}