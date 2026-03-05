using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingSuite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInvoiceItemColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HsnCode",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HsnCode",
                table: "InvoiceItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HsnCode",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "HsnCode",
                table: "InvoiceItems");
        }
    }
}
