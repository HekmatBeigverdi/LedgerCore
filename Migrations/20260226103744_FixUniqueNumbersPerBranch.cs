using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Migrations
{
    /// <inheritdoc />
    public partial class FixUniqueNumbersPerBranch : Migration
    {
        /// <inheritdoc />

        private static void DropIndexIfExists(MigrationBuilder migrationBuilder, string table, string index)
        {
            // MySQL: dropping a non-existing index throws. Also FK constraints may require certain indexes.
            // This helper safely drops an index only if it exists in the current database schema.
            migrationBuilder.Sql($@"
                SET @__idx := (
                    SELECT COUNT(1)
                    FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = '{table}'
                      AND INDEX_NAME = '{index}'
                );
                SET @__sql := IF(@__idx > 0, 'DROP INDEX `{index}` ON `{table}`;', 'SELECT 1;');
                PREPARE stmt FROM @__sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                ");
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE (MySQL): Do NOT drop BranchId indexes that are required by FK constraints.
            // We only replace the global-unique Number indexes with per-branch unique (BranchId, Number) indexes.

            DropIndexIfExists(migrationBuilder, "SalesInvoices", "IX_SalesInvoices_Number");

            DropIndexIfExists(migrationBuilder, "Receipts", "IX_Receipts_Number");

            DropIndexIfExists(migrationBuilder, "PurchaseInvoices", "IX_PurchaseInvoices_Number");

            DropIndexIfExists(migrationBuilder, "PayrollDocuments", "IX_PayrollDocuments_Number");

            DropIndexIfExists(migrationBuilder, "Payments", "IX_Payments_Number");

            DropIndexIfExists(migrationBuilder, "JournalVouchers", "IX_JournalVouchers_Number");

            DropIndexIfExists(migrationBuilder, "InventoryAdjustments", "IX_InventoryAdjustments_Number");

            DropIndexIfExists(migrationBuilder, "CashTransfers", "IX_CashTransfers_Number");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_BranchId_Number",
                table: "SalesInvoices",
                columns: new[] { "BranchId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_BranchId_Number",
                table: "Receipts",
                columns: new[] { "BranchId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_BranchId_Number",
                table: "PurchaseInvoices",
                columns: new[] { "BranchId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDocuments_BranchId_Number",
                table: "PayrollDocuments",
                columns: new[] { "BranchId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BranchId_Number",
                table: "Payments",
                columns: new[] { "BranchId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalVouchers_BranchId_Number",
                table: "JournalVouchers",
                columns: new[] { "BranchId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_BranchId_Number",
                table: "InventoryAdjustments",
                columns: new[] { "BranchId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashTransfers_BranchId_Number",
                table: "CashTransfers",
                columns: new[] { "BranchId", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropIndexIfExists(migrationBuilder, "SalesInvoices", "IX_SalesInvoices_BranchId_Number");

            DropIndexIfExists(migrationBuilder, "Receipts", "IX_Receipts_BranchId_Number");

            DropIndexIfExists(migrationBuilder, "PurchaseInvoices", "IX_PurchaseInvoices_BranchId_Number");

            DropIndexIfExists(migrationBuilder, "PayrollDocuments", "IX_PayrollDocuments_BranchId_Number");

            DropIndexIfExists(migrationBuilder, "Payments", "IX_Payments_BranchId_Number");

            DropIndexIfExists(migrationBuilder, "JournalVouchers", "IX_JournalVouchers_BranchId_Number");

            DropIndexIfExists(migrationBuilder, "InventoryAdjustments", "IX_InventoryAdjustments_BranchId_Number");

            DropIndexIfExists(migrationBuilder, "CashTransfers", "IX_CashTransfers_BranchId_Number");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_BranchId",
                table: "SalesInvoices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_Number",
                table: "SalesInvoices",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_BranchId",
                table: "Receipts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_Number",
                table: "Receipts",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_BranchId",
                table: "PurchaseInvoices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_Number",
                table: "PurchaseInvoices",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDocuments_BranchId",
                table: "PayrollDocuments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDocuments_Number",
                table: "PayrollDocuments",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BranchId",
                table: "Payments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Number",
                table: "Payments",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalVouchers_BranchId",
                table: "JournalVouchers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalVouchers_Number",
                table: "JournalVouchers",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_BranchId",
                table: "InventoryAdjustments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_Number",
                table: "InventoryAdjustments",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashTransfers_BranchId",
                table: "CashTransfers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransfers_Number",
                table: "CashTransfers",
                column: "Number",
                unique: true);
        }
    }
}