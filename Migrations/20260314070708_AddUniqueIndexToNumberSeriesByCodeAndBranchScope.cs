using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToNumberSeriesByCodeAndBranchScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                    UPDATE NumberSeries
                    SET Code = TRIM(Code)
                    WHERE Code IS NOT NULL;
            ");

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
        }
    }
}
