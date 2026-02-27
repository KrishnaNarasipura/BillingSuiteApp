using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingSuite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomerAddressColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename Address column to BillingAddress
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Customers",
                newName: "BillingAddress");

            // Add new ShippingAddress column
            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove ShippingAddress column
            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "Customers");

            // Rename BillingAddress back to Address
            migrationBuilder.RenameColumn(
                name: "BillingAddress",
                table: "Customers",
                newName: "Address");
        }
    }
}