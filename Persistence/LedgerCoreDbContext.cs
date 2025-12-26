using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Assets;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.Models.Payroll;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.Models.Settings;
using LedgerCore.Core.Models.Workflow;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence;

public class LedgerCoreDbContext(DbContextOptions<LedgerCoreDbContext> options) : DbContext(options)
{
    // Master
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<PartyCategory> PartyCategories => Set<PartyCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

    // Accounting
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountGroup> AccountGroups => Set<AccountGroup>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<JournalVoucher> JournalVouchers => Set<JournalVoucher>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<PostingRule> PostingRules => Set<PostingRule>();
    public DbSet<TrialBalanceRow> TrialBalanceRows => Set<TrialBalanceRow>();

    // Inventory
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMove> StockMoves => Set<StockMove>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();

    // Documents
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CashTransfer> CashTransfers => Set<CashTransfer>();
    public DbSet<Cheque> Cheques => Set<Cheque>();
    public DbSet<ChequeHistory> ChequeHistories => Set<ChequeHistory>();

    // Assets
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<DepreciationMethod> DepreciationMethods => Set<DepreciationMethod>();
    public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
    public DbSet<DepreciationSchedule> DepreciationSchedules => Set<DepreciationSchedule>();
    public DbSet<AssetTransaction> AssetTransactions => Set<AssetTransaction>();

    // Payroll
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollItemType> PayrollItemTypes => Set<PayrollItemType>();
    public DbSet<PayrollDocument> PayrollDocuments => Set<PayrollDocument>();
    public DbSet<PayrollLine> PayrollLines => Set<PayrollLine>();

    // Security
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // Settings
    public DbSet<NumberSeries> NumberSeries => Set<NumberSeries>();
    public DbSet<AccountingSettings> AccountingSettings => Set<AccountingSettings>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    // Workflow
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    
    // Logs
    public DbSet<SecurityActivityLog> SecurityActivityLogs => Set<SecurityActivityLog>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // اعمال تنظیمات از کلاس‌های Configuration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerCoreDbContext).Assembly);

        // چند تنظیم مهم عمومی:

        // کلید مرکب برای StockItem (هر کالا در هر انبار یک رکورد)
        modelBuilder.Entity<StockItem>()
            .HasIndex(x => new { x.WarehouseId, x.ProductId })
            .IsUnique();

        // UserName و Email باید یکتا باشند
        modelBuilder.Entity<User>()
            .HasIndex(x => x.UserName)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();
        
        modelBuilder.Entity<TrialBalanceRow>(eb =>
        {
            eb.HasNoKey();
            eb.ToView("vw_TrialBalanceRows"); // adjust view name or remove if not a view
        });
    }
    private async Task ValidateFiscalLocksAsync(CancellationToken cancellationToken)
    {
        // هر JournalVoucher جدید/ویرایش‌شده که Posted است را چک می‌کنیم
        var postedJournals = ChangeTracker.Entries<JournalVoucher>()
            .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified)
                        && e.Entity.Status == DocumentStatus.Posted)
            .Select(e => e.Entity)
            .ToList();

        if (postedJournals.Count == 0)
            return;

        foreach (var j in postedJournals)
        {
            var date = j.Date.Date;

            // سال مالی بسته؟
            var year = await FiscalYears.FirstOrDefaultAsync(
                y => y.StartDate.Date <= date && y.EndDate.Date >= date,
                cancellationToken);

            if (year != null && year.IsClosed)
                throw new InvalidOperationException($"Fiscal year '{year.Name}' is closed. Posting is not allowed on {date:yyyy-MM-dd}.");

            // دوره بسته؟
            var period = await FiscalPeriods.FirstOrDefaultAsync(
                p => p.StartDate.Date <= date && p.EndDate.Date >= date,
                cancellationToken);

            if (period != null && period.IsClosed)
                throw new InvalidOperationException($"Fiscal period '{period.Name}' is closed. Posting is not allowed on {date:yyyy-MM-dd}.");

            // اگر FiscalPeriodId ست نشده بود، اینجا ست می‌کنیم (کمک می‌کند گزارش‌ها دقیق‌تر شوند)
            if (j.FiscalPeriodId == null && period != null)
                j.FiscalPeriodId = period.Id;
        }
    }  
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await ValidateFiscalLocksAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }
}


