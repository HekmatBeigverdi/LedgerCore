using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Migrations
{
    /// <inheritdoc />
    public partial class MakeBranchRequiredForCoreDocs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) MySQL-safe: drop FK if exists (because FK names may differ or FK may not exist)
            DropFkIfExists(migrationBuilder, "fixedassets", "BranchId");
            DropFkIfExists(migrationBuilder, "inventoryadjustments", "BranchId");
            DropFkIfExists(migrationBuilder, "journalvouchers", "BranchId");
            DropFkIfExists(migrationBuilder, "payments", "BranchId");
            DropFkIfExists(migrationBuilder, "payrolldocuments", "BranchId");
            DropFkIfExists(migrationBuilder, "purchaseinvoices", "BranchId");
            DropFkIfExists(migrationBuilder, "receipts", "BranchId");
            DropFkIfExists(migrationBuilder, "salesinvoices", "BranchId");

            // 2) Ensure NULL BranchId rows are set before making columns NOT NULL
            // HeadOffice branch if exists, otherwise first branch
            UpdateNullBranchId(migrationBuilder, "journalvouchers");
            UpdateNullBranchId(migrationBuilder, "salesinvoices");
            UpdateNullBranchId(migrationBuilder, "purchaseinvoices");
            UpdateNullBranchId(migrationBuilder, "receipts");
            UpdateNullBranchId(migrationBuilder, "payments");
            UpdateNullBranchId(migrationBuilder, "inventoryadjustments");
            UpdateNullBranchId(migrationBuilder, "payrolldocuments");
            UpdateNullBranchId(migrationBuilder, "fixedassets");

            // 3) Make BranchId NOT NULL (use actual lowercase table names)
            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "salesinvoices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "receipts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "purchaseinvoices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "payrolldocuments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "payments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "journalvouchers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "inventoryadjustments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "fixedassets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // 4) Re-add FK (use lowercase table names)
            migrationBuilder.AddForeignKey(
                name: "FK_fixedassets_branches_BranchId",
                table: "fixedassets",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_inventoryadjustments_branches_BranchId",
                table: "inventoryadjustments",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_journalvouchers_branches_BranchId",
                table: "journalvouchers",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_branches_BranchId",
                table: "payments",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payrolldocuments_branches_BranchId",
                table: "payrolldocuments",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_purchaseinvoices_branches_BranchId",
                table: "purchaseinvoices",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_receipts_branches_BranchId",
                table: "receipts",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_salesinvoices_branches_BranchId",
                table: "salesinvoices",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop FK if exists (MySQL-safe)
            DropFkIfExists(migrationBuilder, "fixedassets", "BranchId");
            DropFkIfExists(migrationBuilder, "inventoryadjustments", "BranchId");
            DropFkIfExists(migrationBuilder, "journalvouchers", "BranchId");
            DropFkIfExists(migrationBuilder, "payments", "BranchId");
            DropFkIfExists(migrationBuilder, "payrolldocuments", "BranchId");
            DropFkIfExists(migrationBuilder, "purchaseinvoices", "BranchId");
            DropFkIfExists(migrationBuilder, "receipts", "BranchId");
            DropFkIfExists(migrationBuilder, "salesinvoices", "BranchId");

            // Make BranchId nullable again
            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "salesinvoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "receipts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "purchaseinvoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "payrolldocuments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "payments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "journalvouchers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "inventoryadjustments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "fixedassets",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Re-add FK without onDelete behavior (similar to original Down)
            migrationBuilder.AddForeignKey(
                name: "FK_fixedassets_branches_BranchId",
                table: "fixedassets",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_inventoryadjustments_branches_BranchId",
                table: "inventoryadjustments",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_journalvouchers_branches_BranchId",
                table: "journalvouchers",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_branches_BranchId",
                table: "payments",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_payrolldocuments_branches_BranchId",
                table: "payrolldocuments",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_purchaseinvoices_branches_BranchId",
                table: "purchaseinvoices",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_receipts_branches_BranchId",
                table: "receipts",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_salesinvoices_branches_BranchId",
                table: "salesinvoices",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id");
        }

        private static void DropFkIfExists(MigrationBuilder migrationBuilder, string tableName, string columnName)
        {
            // MySQL: find FK by table+column, then drop it if exists.
            migrationBuilder.Sql($@"
            SET @fk := (
                SELECT CONSTRAINT_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = '{tableName}'
                  AND COLUMN_NAME = '{columnName}'
                  AND REFERENCED_TABLE_NAME IS NOT NULL
                LIMIT 1
            );

            SET @sql := IF(@fk IS NULL, 'SELECT 1;', CONCAT('ALTER TABLE `{tableName}` DROP FOREIGN KEY `', @fk, '`;'));
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
            ");
                    }

                    private static void UpdateNullBranchId(MigrationBuilder migrationBuilder, string tableName)
                    {
                        migrationBuilder.Sql($@"
            UPDATE `{tableName}`
            SET `BranchId` = (
                SELECT IFNULL(
                    (SELECT `Id` FROM `branches` WHERE `IsHeadOffice` = 1 ORDER BY `Id` LIMIT 1),
                    (SELECT `Id` FROM `branches` ORDER BY `Id` LIMIT 1)
                )
            )
            WHERE `BranchId` IS NULL;
            ");
                    }
                }
            }
