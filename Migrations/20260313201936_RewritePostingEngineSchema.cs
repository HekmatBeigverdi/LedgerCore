using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Migrations
{
    /// <inheritdoc />
    public partial class RewritePostingEngineSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostingRules_Accounts_CreditAccountId",
                table: "PostingRules");

            migrationBuilder.DropForeignKey(
                name: "FK_PostingRules_Accounts_DebitAccountId",
                table: "PostingRules");

            migrationBuilder.DropForeignKey(
                name: "FK_PostingRules_Accounts_DiscountAccountId",
                table: "PostingRules");

            migrationBuilder.DropForeignKey(
                name: "FK_PostingRules_Accounts_TaxAccountId",
                table: "PostingRules");

            migrationBuilder.DropIndex(
                name: "IX_PostingRules_CreditAccountId",
                table: "PostingRules");

            migrationBuilder.DropIndex(
                name: "IX_PostingRules_DebitAccountId",
                table: "PostingRules");

            migrationBuilder.DropIndex(
                name: "IX_PostingRules_DiscountAccountId",
                table: "PostingRules");

            migrationBuilder.DropIndex(
                name: "IX_PostingRules_DocumentType",
                table: "PostingRules");

            migrationBuilder.DropColumn(
                name: "CreditAccountId",
                table: "PostingRules");

            migrationBuilder.DropColumn(
                name: "DebitAccountId",
                table: "PostingRules");

            migrationBuilder.DropColumn(
                name: "DiscountAccountId",
                table: "PostingRules");

            migrationBuilder.RenameColumn(
                name: "TaxAccountId",
                table: "PostingRules",
                newName: "BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_PostingRules_TaxAccountId",
                table: "PostingRules",
                newName: "IX_PostingRules_BranchId");

            migrationBuilder.AddColumn<bool>(
                name: "AutoPost",
                table: "PostingRules",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "PostingRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PostingRuleLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PostingRuleId = table.Column<int>(type: "int", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    Side = table.Column<int>(type: "int", nullable: false),
                    AmountSource = table.Column<int>(type: "int", nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    UsePartyFromDocument = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DescriptionTemplate = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingRuleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostingRuleLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostingRuleLines_PostingRules_PostingRuleId",
                        column: x => x.PostingRuleId,
                        principalTable: "PostingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_DocumentType_BranchId_Priority",
                table: "PostingRules",
                columns: new[] { "DocumentType", "BranchId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_PostingRuleLines_AccountId",
                table: "PostingRuleLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRuleLines_PostingRuleId_LineNumber",
                table: "PostingRuleLines",
                columns: new[] { "PostingRuleId", "LineNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PostingRules_Branches_BranchId",
                table: "PostingRules",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostingRules_Branches_BranchId",
                table: "PostingRules");

            migrationBuilder.DropTable(
                name: "PostingRuleLines");

            migrationBuilder.DropIndex(
                name: "IX_PostingRules_DocumentType_BranchId_Priority",
                table: "PostingRules");

            migrationBuilder.DropColumn(
                name: "AutoPost",
                table: "PostingRules");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "PostingRules");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "PostingRules",
                newName: "TaxAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_PostingRules_BranchId",
                table: "PostingRules",
                newName: "IX_PostingRules_TaxAccountId");

            migrationBuilder.AddColumn<int>(
                name: "CreditAccountId",
                table: "PostingRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DebitAccountId",
                table: "PostingRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiscountAccountId",
                table: "PostingRules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_CreditAccountId",
                table: "PostingRules",
                column: "CreditAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_DebitAccountId",
                table: "PostingRules",
                column: "DebitAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_DiscountAccountId",
                table: "PostingRules",
                column: "DiscountAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_DocumentType",
                table: "PostingRules",
                column: "DocumentType");

            migrationBuilder.AddForeignKey(
                name: "FK_PostingRules_Accounts_CreditAccountId",
                table: "PostingRules",
                column: "CreditAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PostingRules_Accounts_DebitAccountId",
                table: "PostingRules",
                column: "DebitAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PostingRules_Accounts_DiscountAccountId",
                table: "PostingRules",
                column: "DiscountAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PostingRules_Accounts_TaxAccountId",
                table: "PostingRules",
                column: "TaxAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
