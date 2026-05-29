using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankLite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataIntegrityIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_UserId",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_CreatedAt",
                table: "Transactions",
                columns: new[] { "AccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_Type_CreatedAt",
                table: "Transactions",
                columns: new[] { "AccountId", "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId_Type",
                table: "Accounts",
                columns: new[] { "UserId", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_CreatedAt",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_Type_CreatedAt",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_UserId_Type",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId",
                table: "Accounts",
                column: "UserId");
        }
    }
}
