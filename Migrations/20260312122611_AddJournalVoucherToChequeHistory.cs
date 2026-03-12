using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalVoucherToChequeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ChangedBy",
                table: "ChequeHistories",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "JournalVoucherId",
                table: "ChequeHistories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChequeHistories_JournalVoucherId",
                table: "ChequeHistories",
                column: "JournalVoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChequeHistories_JournalVouchers_JournalVoucherId",
                table: "ChequeHistories",
                column: "JournalVoucherId",
                principalTable: "JournalVouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChequeHistories_JournalVouchers_JournalVoucherId",
                table: "ChequeHistories");

            migrationBuilder.DropIndex(
                name: "IX_ChequeHistories_JournalVoucherId",
                table: "ChequeHistories");

            migrationBuilder.DropColumn(
                name: "JournalVoucherId",
                table: "ChequeHistories");

            migrationBuilder.AlterColumn<string>(
                name: "ChangedBy",
                table: "ChequeHistories",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
