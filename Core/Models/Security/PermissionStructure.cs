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
            }
        };
}
