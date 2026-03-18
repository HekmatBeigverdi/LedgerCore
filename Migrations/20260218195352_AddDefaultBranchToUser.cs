using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultBranchToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_JournalVouchers_JournalVoucherId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_JournalVouchers_JournalVoucherId",
                table: "Receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_JournalVouchers_JournalVoucherId",
                table: "SalesInvoices");

            migrationBuilder.AddColumn<int>(
                name: "DefaultBranchId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReversalJournalVoucherId",
                table: "SalesInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReversalJournalVoucherId",
                table: "Receipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReversalJournalVoucherId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DefaultBranchId",
                table: "Users",
                column: "DefaultBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_ReversalJournalVoucherId",
                table: "SalesInvoices",
                column: "ReversalJournalVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_ReversalJournalVoucherId",
                table: "Receipts",
                column: "ReversalJournalVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReversalJournalVoucherId",
                table: "Payments",
                column: "ReversalJournalVoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_JournalVouchers_JournalVoucherId",
                table: "Payments",
                column: "JournalVoucherId",
                principalTable: "JournalVouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_JournalVouchers_ReversalJournalVoucherId",
                table: "Payments",
                column: "ReversalJournalVoucherId",
                principalTable: "JournalVouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_JournalVouchers_JournalVoucherId",
                table: "Receipts",
                column: "JournalVoucherId",
                principalTable: "JournalVouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_JournalVouchers_ReversalJournalVoucherId",
                table: "Receipts",
                column: "ReversalJournalVoucherId",
                principalTable: "JournalVouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_JournalVouchers_JournalVoucherId",
                table: "SalesInvoices",
                column: "JournalVoucherId",
                principalTable: "JournalVouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_JournalVouchers_ReversalJournalVoucherId",
                table: "SalesInvoices",
                column: "ReversalJournalVoucherId",
                principalTable: "JournalVouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Branches_DefaultBranchId",
                table: "Users",
                column: "DefaultBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_JournalVouchers_JournalVoucherId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_JournalVouchers_ReversalJournalVoucherId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_JournalVouchers_JournalVoucherId",
                table: "Receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_JournalVouchers_ReversalJournalVoucherId",
                table: "Receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_JournalVouchers_JournalVoucherId",
                table: "SalesInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_JournalVouchers_ReversalJournalVoucherId",
                table: "SalesInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Branches_DefaultBranchId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DefaultBranchId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_ReversalJournalVoucherId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_ReversalJournalVoucherId",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ReversalJournalVoucherId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DefaultBranchId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReversalJournalVoucherId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ReversalJournalVoucherId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ReversalJournalVoucherId",
                table: "Payments");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_JournalVouchers_JournalVoucherId",
                table: "Payments",
                column: "JournalVoucherId",
                principalTable: "JournalVouchers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_JournalVouchers_JournalVoucherId",
                table: "Receipts",
                column: "JournalVoucherId",
                principalTable: "JournalVouchers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_JournalVouchers_JournalVoucherId",
                table: "SalesInvoices",
                column: "JournalVoucherId",
                principalTable: "JournalVouchers",
                principalColumn: "Id");
        }
    }
}
