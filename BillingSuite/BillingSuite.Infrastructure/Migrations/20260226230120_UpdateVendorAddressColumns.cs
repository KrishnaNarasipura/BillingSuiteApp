using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingSuite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVendorAddressColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename Address column to BillingAddress
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Vendors",
                newName: "BillingAddress");

            // Add new ShippingAddress column
            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove ShippingAddress column
            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "Vendors");

            // Rename BillingAddress back to Address
            migrationBuilder.RenameColumn(
                name: "BillingAddress",
                table: "Vendors",
                newName: "Address");
        }
    }
}