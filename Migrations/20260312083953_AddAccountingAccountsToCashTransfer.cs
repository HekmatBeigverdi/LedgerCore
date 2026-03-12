using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingAccountsToCashTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FromAccountId",
                table: "CashTransfers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ToAccountId",
                table: "CashTransfers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CashTransfers_FromAccountId",
                table: "CashTransfers",
                column: "FromAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransfers_ToAccountId",
                table: "CashTransfers",
                column: "ToAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransfers_Accounts_FromAccountId",
                table: "CashTransfers",
                column: "FromAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransfers_Accounts_ToAccountId",
                table: "CashTransfers",
                column: "ToAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashTransfers_Accounts_FromAccountId",
                table: "CashTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_CashTransfers_Accounts_ToAccountId",
                table: "CashTransfers");

            migrationBuilder.DropIndex(
                name: "IX_CashTransfers_FromAccountId",
                table: "CashTransfers");

            migrationBuilder.DropIndex(
                name: "IX_CashTransfers_ToAccountId",
                table: "CashTransfers");

            migrationBuilder.DropColumn(
                name: "FromAccountId",
                table: "CashTransfers");

            migrationBuilder.DropColumn(
                name: "ToAccountId",
                table: "CashTransfers");
        }
    }
}
