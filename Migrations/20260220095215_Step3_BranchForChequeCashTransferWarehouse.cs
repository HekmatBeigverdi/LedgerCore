using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Migrations
{
    /// <inheritdoc />
    public partial class Step3BranchForChequeCashTransferWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Cheques",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "CashTransfers",
                type: "int",
                nullable: false,
                defaultValue: 0);
            
            migrationBuilder.Sql(@"
            UPDATE `Cheques`
            SET `BranchId` = (
                SELECT IFNULL(
                    (SELECT `Id` FROM `branches` WHERE `IsHeadOffice` = 1 ORDER BY `Id` LIMIT 1),
                    (SELECT `Id` FROM `branches` ORDER BY `Id` LIMIT 1)
                )
            )
            WHERE `BranchId` IS NULL OR `BranchId` = 0;
            ");
            migrationBuilder.Sql(@"
            UPDATE `CashTransfers`
            SET `BranchId` = (
                SELECT IFNULL(
                    (SELECT `Id` FROM `branches` WHERE `IsHeadOffice` = 1 ORDER BY `Id` LIMIT 1),
                    (SELECT `Id` FROM `branches` ORDER BY `Id` LIMIT 1)
                )
            )
            WHERE `BranchId` IS NULL OR `BranchId` = 0;
            ");
            migrationBuilder.Sql(@"
            UPDATE `Warehouses`
            SET `BranchId` = (
                SELECT IFNULL(
                    (SELECT `Id` FROM `branches` WHERE `IsHeadOffice` = 1 ORDER BY `Id` LIMIT 1),
                    (SELECT `Id` FROM `branches` ORDER BY `Id` LIMIT 1)
                )
            )
            WHERE `BranchId` IS NULL OR `BranchId` = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Cheques_BranchId",
                table: "Cheques",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransfers_BranchId",
                table: "CashTransfers",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransfers_Branches_BranchId",
                table: "CashTransfers",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cheques_Branches_BranchId",
                table: "Cheques",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashTransfers_Branches_BranchId",
                table: "CashTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Cheques_Branches_BranchId",
                table: "Cheques");

            migrationBuilder.DropIndex(
                name: "IX_Cheques_BranchId",
                table: "Cheques");

            migrationBuilder.DropIndex(
                name: "IX_CashTransfers_BranchId",
                table: "CashTransfers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Cheques");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "CashTransfers");
        }
    }
}
