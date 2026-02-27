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

    public async Task<List<SalesSummaryDto>> GetSalesSummaryAsync(DateTime from, DateTime to, int? CustomerId, CancellationToken ct = default)
    {
        var q = _db.Invoices.Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to);
        if (CustomerId.HasValue) q = q.Where(i => i.CustomerId == CustomerId.Value);
        if (q == null || q.Count() == 0 )
        {
            return null;
        }
        var data = await q
            .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Subtotal = g.Sum(x => x.Subtotal),
                Tax = g.Sum(x => x.TaxAmount),
                Net = g.Sum(x => x.NetAmount),
                Count = g.Count()
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(ct);

        // Convert to SalesSummaryDto after materializing data
        var result = data.Select(d => new SalesSummaryDto
        {
            Period = new DateTime(d.Year, d.Month, 1),
            Subtotal = d.Subtotal,
            Tax = d.Tax,
            Net = d.Net,
            Count = d.Count
        }).ToList();

        return result;
    }

    public async Task<byte[]> SalesSummaryPdfAsync(DateTime from, DateTime to, int? CustomerId, CancellationToken ct = default)
    {
        var data = await GetSalesSummaryAsync(from, to, CustomerId, ct);

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

                    // Footer with totals
                    var totalSubtotal = data.Sum(x => x.Subtotal);
                    var totalTax = data.Sum(x => x.Tax);
                    var totalNet = data.Sum(x => x.Net);
                    var totalCount = data.Sum(x => x.Count);

                    t.Cell().Text("TOTAL").SemiBold();
                    t.Cell().Text(totalCount.ToString()).SemiBold();
                    t.Cell().Text(totalSubtotal.ToString("0.00")).SemiBold();
                    t.Cell().Text(totalTax.ToString("0.00")).SemiBold();
                    t.Cell().Text(totalNet.ToString("0.00")).SemiBold();
                });
            });
        }).GeneratePdf();
    }
}