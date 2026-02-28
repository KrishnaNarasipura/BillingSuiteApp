using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingSuite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvanceReceivedToInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceReceived",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvanceReceived",
                table: "Invoices");
        }
    }
}
