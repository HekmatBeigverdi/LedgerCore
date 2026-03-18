using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeNumberSeriesUnification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAdjustments_Branches_BranchId",
                table: "InventoryAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollDocuments_Branches_BranchId",
                table: "PayrollDocuments");
            

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "NumberSeries",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDocuments_Date",
                table: "PayrollDocuments",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSeries_Code",
                table: "NumberSeries",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumberSeries_EntityType_BranchId",
                table: "NumberSeries",
                columns: new[] { "EntityType", "BranchId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_NumberSeries_CurrentNumber",
                table: "NumberSeries",
                sql: "`CurrentNumber` >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NumberSeries_Padding",
                table: "NumberSeries",
                sql: "`Padding` > 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_Date",
                table: "InventoryAdjustments",
                column: "Date");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAdjustments_Branches_BranchId",
                table: "InventoryAdjustments",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollDocuments_Branches_BranchId",
                table: "PayrollDocuments",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAdjustments_Branches_BranchId",
                table: "InventoryAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollDocuments_Branches_BranchId",
                table: "PayrollDocuments");

            migrationBuilder.DropIndex(
                name: "IX_PayrollDocuments_Date",
                table: "PayrollDocuments");

            migrationBuilder.DropIndex(
                name: "IX_NumberSeries_Code",
                table: "NumberSeries");

            migrationBuilder.DropIndex(
                name: "IX_NumberSeries_EntityType_BranchId",
                table: "NumberSeries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NumberSeries_CurrentNumber",
                table: "NumberSeries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NumberSeries_Padding",
                table: "NumberSeries");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAdjustments_Date",
                table: "InventoryAdjustments");
            

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "NumberSeries",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAdjustments_Branches_BranchId",
                table: "InventoryAdjustments",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollDocuments_Branches_BranchId",
                table: "PayrollDocuments",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
