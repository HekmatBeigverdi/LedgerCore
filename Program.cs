
using System.Text;
using LedgerCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

const string secretKey = "iNfgDmHLpUA552sqsjhqGbMRdRj5PVbH"; // todo: get this from somewhere secure
var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("Published");

builder.Services.AddDbContext<LedgerCoreDbContext>(
    dbContextOptions => dbContextOptions
        .UseMySql(connectionString,  ServerVersion.AutoDetect(connectionString))
        // The following three options help with debugging, but should
        // be changed or removed for production.
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors()
);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();