// Services/ReportService.cs
using BillingSuite.Application.Abstractions;
using BillingSuite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BillingSuite.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly BillingDbContext _db;
    public ReportService(BillingDbContext db) => _db = db;

    public async Task<byte[]> SalesSummaryPdfAsync(DateTime from, DateTime to, int? vendorId, CancellationToken ct = default)
    {
        var q = _db.Invoices.Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to);
        if (vendorId.HasValue) q = q.Where(i => i.VendorId == vendorId.Value);

        var data = await q
            .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
            .Select(g => new
            {
                Period = new DateTime(g.Key.Year, g.Key.Month, 1),
                Subtotal = g.Sum(x => x.Subtotal),
                Tax = g.Sum(x => x.TaxAmount),
                Net = g.Sum(x => x.NetAmount),
                Count = g.Count()
            })
            .OrderBy(x => x.Period)
            .ToListAsync(ct);

        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(c =>
        {
            c.Page(p =>
            {
                p.Size(PageSizes.A4);
                p.Margin(20);
                p.Header().Text($"Sales Summary ({from:dd-MMM-yyyy} to {to:dd-MMM-yyyy})").FontSize(14).SemiBold();
                p.Content().Table(t =>
                {
                    t.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3); // Period
                        cols.RelativeColumn(2); // Count
                        cols.RelativeColumn(3); // Subtotal
                        cols.RelativeColumn(2); // Tax
                        cols.RelativeColumn(3); // Net
                    });

                    t.Header(h =>
                    {
                        h.Cell().Text("Period").SemiBold();
                        h.Cell().Text("Invoices").SemiBold();
                        h.Cell().Text("Subtotal").SemiBold();
                        h.Cell().Text("Tax").SemiBold();
                        h.Cell().Text("Net").SemiBold();
                    });

                    foreach (var row in data)
                    {
                        t.Cell().Text(row.Period.ToString("MMM yyyy"));
                        t.Cell().Text(row.Count.ToString());
                        t.Cell().Text(row.Subtotal.ToString("0.00"));
                        t.Cell().Text(row.Tax.ToString("0.00"));
                        t.Cell().Text(row.Net.ToString("0.00"));
                    }
                });
            });
        }).GeneratePdf();
    }
}