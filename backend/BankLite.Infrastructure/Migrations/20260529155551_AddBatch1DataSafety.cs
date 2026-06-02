using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankLite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBatch1DataSafety : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PerformedAt",
                table: "AuditLogs",
                column: "PerformedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_PerformedAt",
                table: "AuditLogs");
        }
    }
}
