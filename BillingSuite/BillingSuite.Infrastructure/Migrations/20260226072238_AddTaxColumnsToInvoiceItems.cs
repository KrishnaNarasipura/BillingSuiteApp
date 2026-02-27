using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingSuite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxColumnsToInvoiceItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaxSettingsId",
                table: "InvoiceItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_TaxSettingsId",
                table: "InvoiceItems",
                column: "TaxSettingsId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_TaxSettings_TaxSettingsId",
                table: "InvoiceItems",
                column: "TaxSettingsId",
                principalTable: "TaxSettings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_TaxSettings_TaxSettingsId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_TaxSettingsId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "TaxSettingsId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "InvoiceItems");
        }
    }
}