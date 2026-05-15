using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankLite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionCreatedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CreatedAt",
                table: "Transactions",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_CreatedAt",
                table: "Transactions");
        }
    }
}
