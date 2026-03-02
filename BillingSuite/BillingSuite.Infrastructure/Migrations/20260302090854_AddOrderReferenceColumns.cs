using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingSuite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderReferenceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OurOrderReference",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "YourOrderReference",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OurOrderReference",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "YourOrderReference",
                table: "Invoices");
        }
    }
}
