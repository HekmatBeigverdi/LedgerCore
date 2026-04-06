using System.Collections.Generic;

namespace LedgerCore.Core.Models.Security;

public static class PermissionSeedData
{
    /// <summary>
    /// تمام Permissionهایی که سیستم به صورت پیش‌فرض نیاز دارد.
    /// </summary>
    public static IReadOnlyList<Permission> GetAll()
        => new List<Permission>
        {
            // ================= Dashboard =================
            new()
            {
                Code = PermissionCodes.Dashboard_View,
                Name = "مشاهده داشبورد",
                Description = "امکان مشاهده داشبورد اصلی سیستم"
            },
            new()
            {
                Code = PermissionCodes.Dashboard_BranchSummary_View,
                Name = "مشاهده خلاصه شعب",
                Description = "امکان مشاهده داشبورد خلاصه وضعیت شعب"
            },

            // ================= Sales =================
            new()
            {
                Code = PermissionCodes.Sales_Invoice_View,
                Name = "مشاهده فاکتورهای فروش",
                Description = "امکان مشاهده لیست و جزئیات فاکتورهای فروش"
            },
            new()
            {
                Code = PermissionCodes.Sales_Return_View,
                Name = "مشاهده برگشت فروش",
                Description = "امکان مشاهده لیست و جزئیات اسناد برگشت فروش"
            },
            new()
            {
                Code = PermissionCodes.Sales_Return_Create,
                Name = "ایجاد برگشت فروش",
                Description = "امکان ثبت سند برگشت فروش"
            },
            new()
            {
                Code = PermissionCodes.Sales_Return_Edit,
                Name = "ویرایش برگشت فروش",
                Description = "امکان ویرایش سند برگشت فروش"
            },
            new()
            {
                Code = PermissionCodes.Sales_Return_Post,
                Name = "پست برگشت فروش",
                Description = "امکان پست‌کردن سند برگشت فروش"
            },

            // ================= Inventory =================
            new()
            {
                Code = PermissionCodes.Inventory_StockItem_View,
                Name = "مشاهده کالاها",
                Description = "امکان مشاهده لیست کالاها و موجودی آنها"
            },
            new()
            {
                Code = PermissionCodes.Inventory_StockCard_View,
                Name = "مشاهده کارتکس کالا",
                Description = "امکان مشاهده ریز گردش (کارتکس) هر کالا"
            },
            new()
            {
                Code = PermissionCodes.Inventory_Adjustment_View,
                Name = "مشاهده تعدیلات انبار",
                Description = "امکان مشاهده اسناد تعدیل موجودی انبار"
            },
            new()
            {
                Code = PermissionCodes.Inventory_Adjustment_Create,
                Name = "ایجاد تعدیل انبار",
                Description = "امکان ثبت سند تعدیل انبار جدید"
            },
            new()
            {
                Code = PermissionCodes.Inventory_Adjustment_Process,
                Name = "پردازش تعدیل انبار",
                Description = "امکان پردازش سند تعدیل و به‌روزرسانی StockMove ها"
            },
            new()
            {
                Code = PermissionCodes.Inventory_Adjustment_Post,
                Name = "پست سند تعدیل",
                Description = "امکان پست‌کردن سند تعدیل انبار به حسابداری"
            },
            new()
            {
                Code = PermissionCodes.Inventory_Transfer_View,
                Name = "مشاهده انتقال بین انبارها",
                Description = "امکان مشاهده اسناد انتقال کالا بین انبارها"
            },
            new()
            {
                Code = PermissionCodes.Inventory_Transfer_Create,
                Name = "ایجاد انتقال بین انبارها",
                Description = "امکان ثبت سند انتقال کالا بین دو انبار"
            },
            new()
            {
                Code = PermissionCodes.Inventory_Transfer_Edit,
                Name = "ویرایش انتقال بین انبارها",
                Description = "امکان ویرایش سند انتقال کالا تا قبل از ثبت نهایی"
            },
            new()
            {
                Code = PermissionCodes.Inventory_Transfer_Post,
                Name = "پست انتقال بین انبارها",
                Description = "امکان ثبت نهایی سند انتقال بین انبارها و اعمال روی موجودی"
            },

            // ================= Reports =================
            new()
            {
                Code = PermissionCodes.Reports_Sales_View,
                Name = "گزارش فروش",
                Description = "امکان مشاهده گزارش‌های فروش"
            },
            new()
            {
                Code = PermissionCodes.Reports_Stock_View,
                Name = "گزارش انبار",
                Description = "امکان مشاهده گزارش‌های موجودی و گردش انبار"
            },
            new()
            {
                Code = PermissionCodes.Reports_TrialBalance_View,
                Name = "گزارش تراز آزمایشی",
                Description = "امکان مشاهده تراز آزمایشی حساب‌ها"
            },
            new()
            {
                Code = PermissionCodes.Reports_FiscalStatus_View,
                Name = "گزارش وضعیت سال و دوره مالی",
                Description = "امکان مشاهده وضعیت سال‌ها و دوره‌های مالی (باز/بسته بودن)"
            },
            new()
            {
                Code = PermissionCodes.Reports_SubLedger_View,
                Name = "گزارش تفصیلی (مانده و گردش)",
                Description = "امکان مشاهده گزارش‌های تفصیلی: مانده تفصیلی و دفتر معین تفصیلی"
            },
            new()
            {
                Code = PermissionCodes.Reports_Aging_View,
                Name = "گزارش سنی بدهی/مطالبات",
                Description = "امکان مشاهده گزارش سنی بدهی/مطالبات برای حساب‌های RequiresParty"
            },
            new()
            {
                Code = PermissionCodes.Reports_Inventory_StockCard_View,
                Name = "گزارش کارتکس کالا",
                Description = "مشاهده گزارش کارتکس/گردش انبار (Stock Card)"
            },
            new()
            {
                Code = PermissionCodes.Reports_Sales_ByParty_View,
                Name = "گزارش فروش به تفکیک طرف حساب",
                Description = "مشاهده گزارش فروش بر اساس Party"
            },
            new()
            {
                Code = PermissionCodes.Reports_Purchases_ByParty_View,
                Name = "گزارش خرید به تفکیک طرف حساب",
                Description = "مشاهده گزارش خرید بر اساس Party"
            },
            new()
            {
                Code = PermissionCodes.Reports_Payroll_Summary_View,
                Name = "گزارش خلاصه حقوق و دستمزد",
                Description = "مشاهده گزارش Summary حقوق و دستمزد"
            },
            new()
            {
                Code = PermissionCodes.Reports_Payroll_Details_View,
                Name = "گزارش ریز حقوق و دستمزد",
                Description = "مشاهده گزارش Details حقوق و دستمزد"
            },
            
            // ================= Approval =================
            new()
            {
                Code = PermissionCodes.Approval_Request_View,
                Name = "مشاهده درخواست‌های تایید",
                Description = "امکان مشاهده لیست درخواست‌های تایید در Workflow"
            },
            new()
            {
                Code = PermissionCodes.Approval_Request_Create,
                Name = "ایجاد درخواست تایید",
                Description = "امکان ثبت درخواست تایید جدید برای اسناد"
            },
            new()
            {
                Code = PermissionCodes.Approval_Request_Approve,
                Name = "تایید درخواست",
                Description = "امکان تایید درخواست‌های در انتظار"
            },
            new()
            {
                Code = PermissionCodes.Approval_Request_Reject,
                Name = "رد درخواست",
                Description = "امکان رد کردن درخواست‌های تایید"
            },

            // ================= Accounting: Journals =================
            new()
            {
                Code = PermissionCodes.Accounting_Journal_View,
                Name = "مشاهده اسناد حسابداری",
                Description = "امکان مشاهده لیست سندهای روزنامه و جزئیات آنها"
            },
            new()
            {
                Code = PermissionCodes.Accounting_Journal_Create,
                Name = "ایجاد سند حسابداری",
                Description = "امکان ثبت سند روزنامه جدید به صورت دستی"
            },
            new()
            {
                Code = PermissionCodes.Accounting_Journal_Edit,
                Name = "ویرایش سند حسابداری",
                Description = "امکان ویرایش سندهای روزنامه در حالت پیش‌نویس"
            },
            new()
            {
                Code = PermissionCodes.Accounting_Journal_Delete,
                Name = "حذف سند حسابداری",
                Description = "امکان حذف سندهای روزنامه در حالت پیش‌نویس"
            },
            new()
            {
                Code = PermissionCodes.Accounting_Journal_Post,
                Name = "پست سند حسابداری",
                Description = "امکان پست‌کردن سند روزنامه و ثبت قطعی آن در دفتر"
            },

            // ================= Accounting: FiscalYear =================
            new()
            {
                Code = PermissionCodes.Accounting_FiscalYear_View,
                Name = "مشاهده سال‌های مالی",
                Description = "امکان مشاهده لیست سال‌های مالی و وضعیت آنها"
            },
            new()
            {
                Code = PermissionCodes.Accounting_FiscalYear_Manage,
                Name = "مدیریت سال‌های مالی",
                Description = "امکان ایجاد و ویرایش سال‌های مالی (تا قبل از بسته شدن)"
            },

            // ================= Accounting: FiscalPeriod =================
            new()
            {
                Code = PermissionCodes.Accounting_FiscalPeriod_View,
                Name = "مشاهده دوره‌های مالی",
                Description = "امکان مشاهده لیست دوره‌ها و وضعیت باز/بسته بودن آنها"
            },
            new()
            {
                Code = PermissionCodes.Accounting_FiscalPeriod_Manage,
                Name = "مدیریت دوره‌های مالی",
                Description = "امکان ایجاد و ویرایش دوره‌های مالی"
            },
            new()
            {
                Code = PermissionCodes.Accounting_FiscalPeriod_Close,
                Name = "بستن دوره مالی",
                Description = "امکان بستن دوره مالی و ثبت سند اختتامیه سود و زیان"
            },
            new()
            {
                Code = PermissionCodes.Accounting_FiscalPeriod_Open,
                Name = "باز کردن دوره مالی",
                Description = "امکان باز کردن مجدد دوره بسته‌شده (Re-open)"
            },
            new()
            {
                Code = PermissionCodes.Sales_Invoice_Create,
                Name = "ایجاد فروش",
                Description = "ثبت فاکتور فروش"
            },
            new()
            {
                Code = PermissionCodes.Sales_Invoice_Edit,
                Name = "ویرایش فروش",
                Description = "ویرایش فاکتور فروش"
            },
            new()
            {
                Code = PermissionCodes.Sales_Invoice_Post,
                Name = "پست فروش",
                Description = "پست فاکتور فروش"
            },

            new()
            {
                Code = PermissionCodes.Payroll_View,
                Name = "مشاهده حقوق",
                Description = "مشاهده اسناد حقوق و دستمزد"
            },
            new()
            {
                Code = PermissionCodes.Payroll_Manage,
                Name = "مدیریت حقوق",
                Description = "ایجاد و محاسبه حقوق"
            },
            new()
            {
                Code = PermissionCodes.Payroll_Post,
                Name = "پست حقوق",
                Description = "پست سند حقوق به حسابداری"
            },

            new()
            {
                Code = PermissionCodes.Assets_View,
                Name = "مشاهده دارایی ثابت",
                Description = "مشاهده دارایی‌ها و گزارشات"
            },
            new()
            {
                Code = PermissionCodes.Assets_Manage,
                Name = "مدیریت دارایی ثابت",
                Description = "ثبت/ویرایش دارایی و برنامه استهلاک"
            },
            new()
            {
                Code = PermissionCodes.Assets_Depreciation_Post,
                Name = "پست استهلاک",
                Description = "پست سند استهلاک به حسابداری"
            },
            // ================= Purchase =================
            new()
            {
                Code = PermissionCodes.Purchase_Invoice_View,
                Name = "مشاهده فاکتورهای خرید",
                Description = "امکان مشاهده لیست و جزئیات فاکتورهای خرید"
            },
            new()
            {
                Code = PermissionCodes.Purchase_Invoice_Create,
                Name = "ایجاد فاکتور خرید",
                Description = "امکان ثبت فاکتور خرید"
            },
            new()
            {
                Code = PermissionCodes.Purchase_Invoice_Edit,
                Name = "ویرایش فاکتور خرید",
                Description = "امکان ویرایش فاکتور خرید"
            },
            new()
            {
                Code = PermissionCodes.Purchase_Invoice_Post,
                Name = "پست فاکتور خرید",
                Description = "امکان پست‌کردن فاکتور خرید"
            },
            new()
            {
                Code = PermissionCodes.Purchase_Return_View,
                Name = "مشاهده برگشت خرید",
                Description = "امکان مشاهده لیست و جزئیات اسناد برگشت خرید"
            },
            new()
            {
                Code = PermissionCodes.Purchase_Return_Create,
                Name = "ایجاد برگشت خرید",
                Description = "امکان ثبت سند برگشت خرید"
            },
            new()
            {
                Code = PermissionCodes.Purchase_Return_Edit,
                Name = "ویرایش برگشت خرید",
                Description = "امکان ویرایش سند برگشت خرید"
            },
            new()
            {
                Code = PermissionCodes.Purchase_Return_Post,
                Name = "پست برگشت خرید",
                Description = "امکان پست‌کردن سند برگشت خرید"
            },

            // ================= Receipts =================
            new()
            {
                Code = PermissionCodes.Receipt_View,
                Name = "مشاهده دریافت‌ها",
                Description = "امکان مشاهده لیست و جزئیات اسناد دریافت"
            },
            new()
            {
                Code = PermissionCodes.Receipt_Create,
                Name = "ایجاد دریافت",
                Description = "امکان ثبت سند دریافت"
            },
            new()
            {
                Code = PermissionCodes.Receipt_Edit,
                Name = "ویرایش دریافت",
                Description = "امکان ویرایش سند دریافت"
            },
            new()
            {
                Code = PermissionCodes.Receipt_Post,
                Name = "پست دریافت",
                Description = "امکان پست‌کردن سند دریافت"
            },
            new()
            {
                Code = PermissionCodes.Receipt_Reverse,
                Name = "برگشت دریافت",
                Description = "امکان ثبت برگشت برای سند دریافت پست‌شده"
            },

            // ================= Payments =================
            new()
            {
                Code = PermissionCodes.Payment_View,
                Name = "مشاهده پرداخت‌ها",
                Description = "امکان مشاهده لیست و جزئیات اسناد پرداخت"
            },
            new()
            {
                Code = PermissionCodes.Payment_Create,
                Name = "ایجاد پرداخت",
                Description = "امکان ثبت سند پرداخت"
            },
            new()
            {
                Code = PermissionCodes.Payment_Edit,
                Name = "ویرایش پرداخت",
                Description = "امکان ویرایش سند پرداخت"
            },
            new()
            {
                Code = PermissionCodes.Payment_Post,
                Name = "پست پرداخت",
                Description = "امکان پست‌کردن سند پرداخت"
            },
            new()
            {
                Code = PermissionCodes.Payment_Reverse,
                Name = "برگشت پرداخت",
                Description = "امکان ثبت برگشت برای سند پرداخت پست‌شده"
            },

            // ================= CashTransfer =================
            new()
            {
                Code = PermissionCodes.CashTransfer_View,
                Name = "مشاهده انتقال وجه",
                Description = "امکان مشاهده اسناد انتقال وجه"
            },
            new()
            {
                Code = PermissionCodes.CashTransfer_Create,
                Name = "ایجاد انتقال وجه",
                Description = "امکان ثبت سند انتقال وجه"
            },
            new()
            {
                Code = PermissionCodes.CashTransfer_Post,
                Name = "پست انتقال وجه",
                Description = "امکان پست‌کردن سند انتقال وجه"
            },

            // ================= Cheques =================
            new()
            {
                Code = PermissionCodes.Cheque_View,
                Name = "مشاهده چک‌ها",
                Description = "امکان مشاهده لیست و جزئیات چک‌ها"
            },
            new()
            {
                Code = PermissionCodes.Cheque_Create,
                Name = "ثبت چک",
                Description = "امکان ثبت چک جدید"
            },
            new()
            {
                Code = PermissionCodes.Cheque_Status_Change,
                Name = "تغییر وضعیت چک",
                Description = "امکان تغییر وضعیت چک"
            },

            // ================= Security =================
            new()
            {
                Code = PermissionCodes.Security_Permissions_View,
                Name = "مشاهده ساختار دسترسی‌ها",
                Description = "امکان مشاهده درخت permissionها"
            },
            new()
            {
                Code = PermissionCodes.Security_Roles_View,
                Name = "مشاهده نقش‌ها",
                Description = "امکان مشاهده نقش‌ها و دسترسی‌های آن‌ها"
            },
            new()
            {
                Code = PermissionCodes.Security_Logs_View,
                Name = "مشاهده لاگ‌های امنیتی",
                Description = "امکان مشاهده لاگ‌های امنیتی سیستم"
            },
            // ================= Fiscal (Close and Open year) =================
            new()
            {
                Code = PermissionCodes.Accounting_Fiscal_Close,
                Name = "بستن سال مالی",
                Description = "امکان بستن سال مالی و تولید سند افتتاحیه سال بعد"
            },
            new()
            {
                Code = PermissionCodes.Accounting_FiscalYear_Open,
                Name = "باز کردن سال مالی",
                Description = "امکان باز کردن مجدد سال مالی بسته‌شده"
            },
            // ================= Masters =================
            new()
            {
                Code = PermissionCodes.Master_Accounts_View,
                Name = "مشاهده حساب‌ها",
                Description = "امکان مشاهده لیست و جزئیات حساب‌ها"
            },
            new()
            {
                Code = PermissionCodes.Master_Accounts_Manage,
                Name = "مدیریت حساب‌ها",
                Description = "امکان ایجاد، ویرایش و مدیریت حساب‌ها"
            },

            new()
            {
                Code = PermissionCodes.Master_Parties_View,
                Name = "مشاهده طرف حساب‌ها",
                Description = "امکان مشاهده لیست و جزئیات طرف حساب‌ها"
            },
            new()
            {
                Code = PermissionCodes.Master_Parties_Manage,
                Name = "مدیریت طرف حساب‌ها",
                Description = "امکان ایجاد، ویرایش و مدیریت طرف حساب‌ها"
            },

            new()
            {
                Code = PermissionCodes.Master_Products_View,
                Name = "مشاهده کالاها",
                Description = "امکان مشاهده لیست و جزئیات کالاها"
            },
            new()
            {
                Code = PermissionCodes.Master_Products_Manage,
                Name = "مدیریت کالاها",
                Description = "امکان ایجاد، ویرایش و مدیریت کالاها"
            },

            new()
            {
                Code = PermissionCodes.Master_Warehouses_View,
                Name = "مشاهده انبارها",
                Description = "امکان مشاهده لیست و جزئیات انبارها"
            },
            new()
            {
                Code = PermissionCodes.Master_Warehouses_Manage,
                Name = "مدیریت انبارها",
                Description = "امکان ایجاد، ویرایش و مدیریت انبارها"
            },

            new()
            {
                Code = PermissionCodes.Master_Branches_View,
                Name = "مشاهده شعب",
                Description = "امکان مشاهده لیست و جزئیات شعب"
            },
            new()
            {
                Code = PermissionCodes.Master_Branches_Manage,
                Name = "مدیریت شعب",
                Description = "امکان ایجاد، ویرایش و مدیریت شعب"
            },

            new()
            {
                Code = PermissionCodes.Master_TaxRates_View,
                Name = "مشاهده نرخ‌های مالیات",
                Description = "امکان مشاهده لیست و جزئیات نرخ‌های مالیات"
            },
            new()
            {
                Code = PermissionCodes.Master_TaxRates_Manage,
                Name = "مدیریت نرخ‌های مالیات",
                Description = "امکان ایجاد، ویرایش و مدیریت نرخ‌های مالیات"
            },

            new()
            {
                Code = PermissionCodes.Master_CostCenters_View,
                Name = "مشاهده مراکز هزینه",
                Description = "امکان مشاهده لیست و جزئیات مراکز هزینه"
            },
            new()
            {
                Code = PermissionCodes.Master_CostCenters_Manage,
                Name = "مدیریت مراکز هزینه",
                Description = "امکان ایجاد، ویرایش و مدیریت مراکز هزینه"
            },

            new()
            {
                Code = PermissionCodes.Master_Projects_View,
                Name = "مشاهده پروژه‌ها",
                Description = "امکان مشاهده لیست و جزئیات پروژه‌ها"
            },
            new()
            {
                Code = PermissionCodes.Master_Projects_Manage,
                Name = "مدیریت پروژه‌ها",
                Description = "امکان ایجاد، ویرایش و مدیریت پروژه‌ها"
            },

            new()
            {
                Code = PermissionCodes.Master_AccountingSettings_View,
                Name = "مشاهده تنظیمات حسابداری",
                Description = "امکان مشاهده تنظیمات حسابداری"
            },
            new()
            {
                Code = PermissionCodes.Master_AccountingSettings_Manage,
                Name = "مدیریت تنظیمات حسابداری",
                Description = "امکان ایجاد، ویرایش و مدیریت تنظیمات حسابداری"
            },

            new()
            {
                Code = PermissionCodes.Master_NumberSeries_View,
                Name = "مشاهده سری شماره‌ها",
                Description = "امکان مشاهده لیست و جزئیات سری شماره‌ها"
            },
            new()
            {
                Code = PermissionCodes.Master_NumberSeries_Manage,
                Name = "مدیریت سری شماره‌ها",
                Description = "امکان ایجاد، ویرایش و مدیریت سری شماره‌ها"
            },

            new()
            {
                Code = PermissionCodes.Master_PostingRules_View,
                Name = "مشاهده قواعد ثبت خودکار",
                Description = "امکان مشاهده لیست و جزئیات قواعد ثبت خودکار"
            },
            new()
            {
                Code = PermissionCodes.Master_PostingRules_Manage,
                Name = "مدیریت قواعد ثبت خودکار",
                Description = "امکان ایجاد، ویرایش و مدیریت قواعد ثبت خودکار"
            },
            
        };
}
