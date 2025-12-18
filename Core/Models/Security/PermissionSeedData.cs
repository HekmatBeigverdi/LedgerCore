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

        };
}
