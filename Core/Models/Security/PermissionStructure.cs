using System.Collections.Generic;

namespace LedgerCore.Core.Models.Security;

public static class PermissionStructure
{
    public static IReadOnlyList<PermissionCategory> GetCategories()
        => new List<PermissionCategory>
        {
            new PermissionCategory
            {
                Key = "dashboard",
                DisplayName = "داشبورد",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Dashboard_View, Name = "مشاهده داشبورد" },
                    new PermissionDefinition { Code = PermissionCodes.Dashboard_BranchSummary_View, Name = "مشاهده خلاصه شعب" }
                }
            },

            new PermissionCategory
            {
                Key = "reports",
                DisplayName = "گزارشات",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Reports_Sales_View, Name = "گزارش فروش" },
                    new PermissionDefinition { Code = PermissionCodes.Reports_Stock_View, Name = "گزارش انبار" },
                    new PermissionDefinition { Code = PermissionCodes.Reports_TrialBalance_View, Name = "تراز آزمایشی" },
                    new PermissionDefinition { Code = PermissionCodes.Reports_FiscalStatus_View, Name = "وضعیت سال/دوره مالی" }
                }
            },

            new PermissionCategory
            {
                Key = "accounting",
                DisplayName = "حسابداری",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Accounting_Journal_View, Name = "مشاهده اسناد حسابداری" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_Journal_Create, Name = "ایجاد سند حسابداری" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_Journal_Edit, Name = "ویرایش سند حسابداری" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_Journal_Delete, Name = "حذف سند حسابداری" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_Journal_Post, Name = "پست سند حسابداری" },

                    new PermissionDefinition { Code = PermissionCodes.Accounting_FiscalYear_View, Name = "مشاهده سال‌های مالی" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_FiscalYear_Manage, Name = "مدیریت سال‌های مالی" },

                    new PermissionDefinition { Code = PermissionCodes.Accounting_FiscalPeriod_View, Name = "مشاهده دوره‌های مالی" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_FiscalPeriod_Manage, Name = "مدیریت دوره‌های مالی" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_FiscalPeriod_Close, Name = "بستن دوره مالی" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_FiscalPeriod_Open, Name = "باز کردن دوره مالی" }
                }
            },

            new PermissionCategory
            {
                Key = "sales",
                DisplayName = "فروش",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Sales_Invoice_View, Name = "مشاهده فاکتورهای فروش" },
                    new PermissionDefinition { Code = PermissionCodes.Sales_Invoice_Create, Name = "ایجاد فاکتور فروش" },
                    new PermissionDefinition { Code = PermissionCodes.Sales_Invoice_Edit, Name = "ویرایش فاکتور فروش" },
                    new PermissionDefinition { Code = PermissionCodes.Sales_Invoice_Post, Name = "پست فاکتور فروش" }
                }
            },
            
            new PermissionCategory
            {
                Key = "purchase",
                DisplayName = "خرید",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Purchase_Invoice_View, Name = "مشاهده فاکتورهای خرید" },
                    new PermissionDefinition { Code = PermissionCodes.Purchase_Invoice_Create, Name = "ایجاد فاکتور خرید" },
                    new PermissionDefinition { Code = PermissionCodes.Purchase_Invoice_Edit, Name = "ویرایش فاکتور خرید" },
                    new PermissionDefinition { Code = PermissionCodes.Purchase_Invoice_Post, Name = "پست فاکتور خرید" }
                }
            },

            new PermissionCategory
            {
                Key = "inventory",
                DisplayName = "انبار",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Inventory_StockItem_View, Name = "مشاهده کالاها" },
                    new PermissionDefinition { Code = PermissionCodes.Inventory_StockCard_View, Name = "مشاهده کارتکس کالا" },
                    new PermissionDefinition { Code = PermissionCodes.Inventory_Adjustment_View, Name = "مشاهده تعدیلات انبار" },
                    new PermissionDefinition { Code = PermissionCodes.Inventory_Adjustment_Create, Name = "ایجاد تعدیل انبار" },
                    new PermissionDefinition { Code = PermissionCodes.Inventory_Adjustment_Process, Name = "پردازش تعدیل انبار" },
                    new PermissionDefinition { Code = PermissionCodes.Inventory_Adjustment_Post, Name = "پست تعدیل انبار" }
                }
            },

            new PermissionCategory
            {
                Key = "payroll",
                DisplayName = "حقوق و دستمزد",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Payroll_View, Name = "مشاهده حقوق و دستمزد" },
                    new PermissionDefinition { Code = PermissionCodes.Payroll_Manage, Name = "مدیریت حقوق و دستمزد" },
                    new PermissionDefinition { Code = PermissionCodes.Payroll_Post, Name = "پست سند حقوق" }
                }
            },
            
            new PermissionCategory
            {
                Key = "treasury",
                DisplayName = "خزانه‌داری",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Receipt_View, Name = "مشاهده دریافت‌ها" },
                    new PermissionDefinition { Code = PermissionCodes.Receipt_Create, Name = "ایجاد دریافت" },
                    new PermissionDefinition { Code = PermissionCodes.Receipt_Edit, Name = "ویرایش دریافت" },
                    new PermissionDefinition { Code = PermissionCodes.Receipt_Post, Name = "پست دریافت" },
                    new PermissionDefinition { Code = PermissionCodes.Receipt_Reverse, Name = "برگشت دریافت" },

                    new PermissionDefinition { Code = PermissionCodes.Payment_View, Name = "مشاهده پرداخت‌ها" },
                    new PermissionDefinition { Code = PermissionCodes.Payment_Create, Name = "ایجاد پرداخت" },
                    new PermissionDefinition { Code = PermissionCodes.Payment_Edit, Name = "ویرایش پرداخت" },
                    new PermissionDefinition { Code = PermissionCodes.Payment_Post, Name = "پست پرداخت" },
                    new PermissionDefinition { Code = PermissionCodes.Payment_Reverse, Name = "برگشت پرداخت" },

                    new PermissionDefinition { Code = PermissionCodes.CashTransfer_View, Name = "مشاهده انتقال وجه" },
                    new PermissionDefinition { Code = PermissionCodes.CashTransfer_Create, Name = "ایجاد انتقال وجه" },
                    new PermissionDefinition { Code = PermissionCodes.CashTransfer_Post, Name = "پست انتقال وجه" },

                    new PermissionDefinition { Code = PermissionCodes.Cheque_View, Name = "مشاهده چک‌ها" },
                    new PermissionDefinition { Code = PermissionCodes.Cheque_Create, Name = "ثبت چک" },
                    new PermissionDefinition { Code = PermissionCodes.Cheque_Status_Change, Name = "تغییر وضعیت چک" }
                }
            },

            new PermissionCategory
            {
                Key = "assets",
                DisplayName = "دارایی ثابت",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Assets_View, Name = "مشاهده دارایی ثابت" },
                    new PermissionDefinition { Code = PermissionCodes.Assets_Manage, Name = "مدیریت دارایی ثابت" },
                    new PermissionDefinition { Code = PermissionCodes.Assets_Depreciation_Post, Name = "پست استهلاک" }
                }
            },

            new PermissionCategory
            {
                Key = "workflow",
                DisplayName = "گردش کار و تایید",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Approval_Request_View, Name = "مشاهده درخواست‌ها" },
                    new PermissionDefinition { Code = PermissionCodes.Approval_Request_Create, Name = "ایجاد درخواست" },
                    new PermissionDefinition { Code = PermissionCodes.Approval_Request_Approve, Name = "تایید درخواست" },
                    new PermissionDefinition { Code = PermissionCodes.Approval_Request_Reject, Name = "رد درخواست" }
                }
            },
            
            new PermissionCategory
            {
                Key = "security",
                DisplayName = "امنیت",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Security_Permissions_View, Name = "مشاهده ساختار دسترسی‌ها" },
                    new PermissionDefinition { Code = PermissionCodes.Security_Roles_View, Name = "مشاهده نقش‌ها و دسترسی نقش‌ها" },
                    new PermissionDefinition { Code = PermissionCodes.Security_Logs_View, Name = "مشاهده لاگ‌های امنیتی" }
                }
            },
            new PermissionCategory
            {
                Key = "masters",
                DisplayName = "اطلاعات پایه",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Master_Accounts_View, Name = "مشاهده حساب‌ها" },
                    new PermissionDefinition { Code = PermissionCodes.Master_Accounts_Manage, Name = "مدیریت حساب‌ها" },

                    new PermissionDefinition { Code = PermissionCodes.Master_Parties_View, Name = "مشاهده طرف حساب‌ها" },
                    new PermissionDefinition { Code = PermissionCodes.Master_Parties_Manage, Name = "مدیریت طرف حساب‌ها" },

                    new PermissionDefinition { Code = PermissionCodes.Master_Products_View, Name = "مشاهده کالاها" },
                    new PermissionDefinition { Code = PermissionCodes.Master_Products_Manage, Name = "مدیریت کالاها" },

                    new PermissionDefinition { Code = PermissionCodes.Master_Warehouses_View, Name = "مشاهده انبارها" },
                    new PermissionDefinition { Code = PermissionCodes.Master_Warehouses_Manage, Name = "مدیریت انبارها" },

                    new PermissionDefinition { Code = PermissionCodes.Master_Branches_View, Name = "مشاهده شعب" },
                    new PermissionDefinition { Code = PermissionCodes.Master_Branches_Manage, Name = "مدیریت شعب" },

                    new PermissionDefinition { Code = PermissionCodes.Master_TaxRates_View, Name = "مشاهده نرخ‌های مالیات" },
                    new PermissionDefinition { Code = PermissionCodes.Master_TaxRates_Manage, Name = "مدیریت نرخ‌های مالیات" },

                    new PermissionDefinition { Code = PermissionCodes.Master_CostCenters_View, Name = "مشاهده مراکز هزینه" },
                    new PermissionDefinition { Code = PermissionCodes.Master_CostCenters_Manage, Name = "مدیریت مراکز هزینه" },

                    new PermissionDefinition { Code = PermissionCodes.Master_Projects_View, Name = "مشاهده پروژه‌ها" },
                    new PermissionDefinition { Code = PermissionCodes.Master_Projects_Manage, Name = "مدیریت پروژه‌ها" },

                    new PermissionDefinition { Code = PermissionCodes.Master_AccountingSettings_View, Name = "مشاهده تنظیمات حسابداری" },
                    new PermissionDefinition { Code = PermissionCodes.Master_AccountingSettings_Manage, Name = "مدیریت تنظیمات حسابداری" },

                    new PermissionDefinition { Code = PermissionCodes.Master_NumberSeries_View, Name = "مشاهده سری شماره‌ها" },
                    new PermissionDefinition { Code = PermissionCodes.Master_NumberSeries_Manage, Name = "مدیریت سری شماره‌ها" },

                    new PermissionDefinition { Code = PermissionCodes.Master_PostingRules_View, Name = "مشاهده قواعد ثبت خودکار" },
                    new PermissionDefinition { Code = PermissionCodes.Master_PostingRules_Manage, Name = "مدیریت قواعد ثبت خودکار" }
                }
            },
        };
}
