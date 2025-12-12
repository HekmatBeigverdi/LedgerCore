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
                    new PermissionDefinition { Code = PermissionCodes.Reports_FiscalStatus_View, Name = "وضعیت سال/دوره مالی" },
                }
            },

            new PermissionCategory
            {
                Key = "accounting",
                DisplayName = "حسابداری",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Accounting_Journal_View, Name = "مشاهده اسناد حسابداری" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_Journal_Create, Name = "ایجاد سند" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_Journal_Edit, Name = "ویرایش سند" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_Journal_Delete, Name = "حذف سند" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_Journal_Post, Name = "پست سند" },

                    new PermissionDefinition { Code = PermissionCodes.Accounting_FiscalYear_View, Name = "مشاهده سال مالی" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_FiscalYear_Manage, Name = "مدیریت سال مالی" },

                    new PermissionDefinition { Code = PermissionCodes.Accounting_FiscalPeriod_View, Name = "مشاهده دوره مالی" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_FiscalPeriod_Manage, Name = "مدیریت دوره مالی" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_FiscalPeriod_Close, Name = "بستن دوره" },
                    new PermissionDefinition { Code = PermissionCodes.Accounting_FiscalPeriod_Open, Name = "بازکردن دوره" },
                }
            },

            new PermissionCategory
            {
                Key = "inventory",
                DisplayName = "انبار",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Inventory_StockItem_View, Name = "مشاهده کالاها" },
                    new PermissionDefinition { Code = PermissionCodes.Inventory_StockCard_View, Name = "کارتکس کالا" },
                    new PermissionDefinition { Code = PermissionCodes.Inventory_Adjustment_View, Name = "مشاهده تعدیلات" },
                    new PermissionDefinition { Code = PermissionCodes.Inventory_Adjustment_Create, Name = "ایجاد تعدیل" },
                    new PermissionDefinition { Code = PermissionCodes.Inventory_Adjustment_Process, Name = "پردازش تعدیل" },
                    new PermissionDefinition { Code = PermissionCodes.Inventory_Adjustment_Post, Name = "پست تعدیل" },
                }
            },

            new PermissionCategory
            {
                Key = "workflow",
                DisplayName = "سازوکار تأیید",
                Permissions =
                {
                    new PermissionDefinition { Code = PermissionCodes.Approval_Request_View, Name = "مشاهده درخواست‌ها" },
                    new PermissionDefinition { Code = PermissionCodes.Approval_Request_Create, Name = "ایجاد درخواست" },
                    new PermissionDefinition { Code = PermissionCodes.Approval_Request_Approve, Name = "تأیید درخواست" },
                    new PermissionDefinition { Code = PermissionCodes.Approval_Request_Reject, Name = "رد درخواست" }
                }
            }
        };
}
