using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Models.Master;

public class Party: AuditableEntity
{
    public string Code { get; set; } = default!;          // کد طرف حساب
    public string Name { get; set; } = default!;          // نام/عنوان
    public PartyType Type { get; set; } = PartyType.Customer;

    public int? CategoryId { get; set; }                  // ارجاع به دسته‌بندی (اختیاری)
    public PartyCategory? Category { get; set; }

    public string? NationalId { get; set; }               // کد ملی / شناسه ملی
    public string? EconomicCode { get; set; }             // کد اقتصادی
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }

    public decimal? CreditLimit { get; set; }             // سقف اعتبار
    public int? DefaultCurrencyId { get; set; }
    public Currency? DefaultCurrency { get; set; }

    public bool IsActive { get; set; } = true;
}