using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Migrations
{
    /// <inheritdoc />
    public partial class AddChequeHistoryCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ChequeHistories_ChequeId_ChangeDate",
                table: "ChequeHistories",
                columns: new[] { "ChequeId", "ChangeDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChequeHistories_ChequeId_ChangeDate",
                table: "ChequeHistories");
        }
    }
}
