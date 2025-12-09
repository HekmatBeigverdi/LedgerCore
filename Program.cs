using System.Text;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.Services;
using LedgerCore.Mapping;
using LedgerCore.Persistence;
using LedgerCore.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging; // added for LogLevel
using Microsoft.AspNetCore.Authentication.JwtBearer;


const string secretKey = "iNfgDmHLpUA552sqsjhqGbMRdRj5PVbH"; // todo: get this from somewhere secure
var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("Published");

var serverVersion = new MySqlServerVersion(new Version(8, 0, 32));


// DbContext
builder.Services.AddDbContext<LedgerCoreDbContext>(
    dbContextOptions => dbContextOptions
        .UseMySql(connectionString, serverVersion, mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure();
            // set command timeout if needed: mysqlOptions.CommandTimeout(60);
        })
        // The following three options help with debugging, but should
        // be changed or removed for production.
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors()
);

// UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Repositories
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IPartyRepository, PartyRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ITaxRateRepository, TaxRateRepository>();
builder.Services.AddScoped<IJournalRepository, JournalRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<ICostCenterRepository, CostCenterRepository>();
builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
builder.Services.AddScoped<IFixedAssetRepository, FixedAssetRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IReceiptRepository, ReceiptRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IChequeRepository, ChequeRepository>();



// Services
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IAccountingService, AccountingService>();
builder.Services.AddScoped<IChequeService, ChequeService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<ICashTransferService, CashTransferService>();





// AutoMapper
builder.Services.AddAutoMapper(typeof(DomainMappingProfile));

// MVC / Controllers
builder.Services.AddControllers();

// OpenAPI / Swagger - keep standard setup (remove unknown AddOpenApi)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(signingKey);


builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,   // همان کلید بالا
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    // مثال: اگر Permission های ثابت می‌شناسی، اینجا Policy برایشان تعریف کن:
    string[] permissions =
    {
        "Sales.Invoice.View",
        "Sales.Invoice.Create",
        "Sales.Invoice.Approve",
        "Reports.TrialBalance.View",
        "Reports.Payroll.View",
        "Dashboard.View",
        "Dashboard.BranchSummary.View",
        
        "Inventory.StockCard.View",
        "Inventory.StockItem.View",
        
        "Inventory.Adjustment.Create",
        "Inventory.Adjustment.View",
        "Inventory.Adjustment.Process",
        "Inventory.Adjustment.Post",
        
        "Approval.Request.Create",
        "Approval.Request.View",
        "Approval.Request.Approve",
        "Approval.Request.Reject"
    };

    foreach (var permission in permissions)
    {
        var policyName = HasPermissionAttribute.BuildPolicyName(permission);

        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("permission", permission);
        });
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();   // ⬅ حتماً قبل از UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();