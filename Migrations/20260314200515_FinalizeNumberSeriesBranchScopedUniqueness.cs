using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeNumberSeriesBranchScopedUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NumberSeries_Code",
                table: "NumberSeries");

            migrationBuilder.DropIndex(
                name: "IX_NumberSeries_EntityType_BranchId",
                table: "NumberSeries");

            migrationBuilder.AddColumn<int>(
                name: "BranchScopeId",
                table: "NumberSeries",
                type: "int",
                nullable: false,
                computedColumnSql: "IFNULL(`BranchId`, 0)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "UX_NumberSeries_Code_BranchScope",
                table: "NumberSeries",
                columns: new[] { "Code", "BranchScopeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_NumberSeries_Code_BranchScope",
                table: "NumberSeries");

            migrationBuilder.DropColumn(
                name: "BranchScopeId",
                table: "NumberSeries");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSeries_Code",
                table: "NumberSeries",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumberSeries_EntityType_BranchId",
                table: "NumberSeries",
                columns: new[] { "EntityType", "BranchId" });
        }
    }
}
