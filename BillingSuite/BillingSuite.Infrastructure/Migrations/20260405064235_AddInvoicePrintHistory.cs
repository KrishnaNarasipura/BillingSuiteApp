using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingSuite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePrintHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoicePrintHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PdfPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PrintedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoicePrintHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoicePrintHistories_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePrintHistories_InvoiceId",
                table: "InvoicePrintHistories",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoicePrintHistories");
        }
    }
}
