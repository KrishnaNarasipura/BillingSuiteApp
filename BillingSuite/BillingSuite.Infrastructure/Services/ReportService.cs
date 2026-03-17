// Services/ReportService.cs
using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace BillingSuite.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly BillingDbContext _db;
    public ReportService(BillingDbContext db) => _db = db;

    public async Task<List<SalesSummaryDto>> GetSalesSummaryAsync(DateTime from, DateTime to, int? CustomerId, CancellationToken ct = default)
    {
        var q = _db.Invoices.Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to);
        if (CustomerId.HasValue) q = q.Where(i => i.CustomerId == CustomerId.Value);
        if (q == null || q.Count() == 0)
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

    public async Task<string> SalesSummaryHtmlAsync(DateTime from, DateTime to, int? CustomerId, CancellationToken ct = default)
    {
        var data = await GetSalesSummaryAsync(from, to, CustomerId, ct);
        var settings = await _db.CompanySettings.FirstOrDefaultAsync(ct);
        var companyName = settings?.CompanyName ?? "BillingSuite";

        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Services", "Html", "Templates", "SalesSummaryTemplate.html");
        var template = await File.ReadAllTextAsync(templatePath, ct);

        // Build table rows
        var sb = new StringBuilder();
        if (data != null)
        {
            foreach (var row in data)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{row.Period:MMM yyyy}</td>");
                sb.Append($"<td class='text-right'>{row.Count}</td>");
                sb.Append($"<td class='text-right'>{row.Subtotal:N2}</td>");
                sb.Append($"<td class='text-right'>{row.Tax:N2}</td>");
                sb.Append($"<td class='text-right'>{row.Net:N2}</td>");
                sb.Append("</tr>");
            }
        }

        var totalCount = data?.Sum(x => x.Count) ?? 0;
        var totalSubtotal = data?.Sum(x => x.Subtotal) ?? 0;
        var totalTax = data?.Sum(x => x.Tax) ?? 0;
        var totalNet = data?.Sum(x => x.Net) ?? 0;

        var placeholders = new Dictionary<string, string>
        {
            { "{{COMPANY_NAME}}", System.Web.HttpUtility.HtmlEncode(companyName) },
            { "{{FROM_DATE}}", from.ToString("dd-MMM-yyyy") },
            { "{{TO_DATE}}", to.ToString("dd-MMM-yyyy") },
            { "{{TABLE_ROWS}}", sb.ToString() },
            { "{{TOTAL_COUNT}}", totalCount.ToString() },
            { "{{TOTAL_SUBTOTAL}}", totalSubtotal.ToString("N2") },
            { "{{TOTAL_TAX}}", totalTax.ToString("N2") },
            { "{{TOTAL_NET}}", totalNet.ToString("N2") },
            { "{{GENERATED_DATE}}", DateTime.Now.ToString("dd-MMM-yyyy HH:mm") }
        };

        var html = template;
        foreach (var kvp in placeholders)
        {
            html = html.Replace(kvp.Key, kvp.Value);
        }

        return html;
    }

    public async Task<List<TaxSummaryDto>> GetTaxSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var data = await _db.InvoiceItems
            .Include(ii => ii.Invoice)
                .ThenInclude(i => i.Customer)
            .Include(ii => ii.TaxSettings)
            .Where(ii => ii.Invoice.InvoiceDate >= from && ii.Invoice.InvoiceDate <= to)
            .Where(ii => ii.TaxSettingsId != null && ii.TaxAmount > 0)
            .OrderBy(ii => ii.Invoice.InvoiceDate)
            .ThenBy(ii => ii.Invoice.InvoiceNumber)
            .Select(ii => new TaxSummaryDto
            {
                InvoiceId = ii.InvoiceId,
                InvoiceNumber = ii.Invoice.InvoiceNumber,
                InvoiceDate = ii.Invoice.InvoiceDate,
                CustomerName = ii.Invoice.Customer.Name,
                Description = ii.Description,
                HsnCode = ii.HsnCode,
                TaxType = ii.TaxSettings != null ? ii.TaxSettings.TaxType : "N/A",
                TaxPercent = ii.TaxSettings != null ? ii.TaxSettings.TaxPercent : 0,
                LineTotal = ii.LineTotal,
                TaxAmount = ii.TaxAmount
            })
            .ToListAsync(ct);

        return data;
    }

    public async Task<string> TaxSummaryHtmlAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var data = await GetTaxSummaryAsync(from, to, ct);
        var settings = await _db.CompanySettings.FirstOrDefaultAsync(ct);
        var companyName = settings?.CompanyName ?? "BillingSuite";

        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Services", "Html", "Templates", "TaxSummaryTemplate.html");
        var template = await File.ReadAllTextAsync(templatePath, ct);

        // Build table rows
        var sb = new StringBuilder();
        foreach (var row in data)
        {
            sb.Append("<tr>");
            sb.Append($"<td>{System.Web.HttpUtility.HtmlEncode(row.InvoiceNumber)}</td>");
            sb.Append($"<td>{row.InvoiceDate:dd-MM-yyyy}</td>");
            sb.Append($"<td>{System.Web.HttpUtility.HtmlEncode(row.CustomerName)}</td>");
            sb.Append($"<td>{System.Web.HttpUtility.HtmlEncode(row.HsnCode)}</td>");
            sb.Append($"<td>{System.Web.HttpUtility.HtmlEncode(row.TaxType)}</td>");
            sb.Append($"<td class='text-center'>{row.TaxPercent:N2}%</td>");
            sb.Append($"<td class='text-right'>{row.LineTotal:N2}</td>");
            sb.Append($"<td class='text-right'>{row.TaxAmount:N2}</td>");
            sb.Append("</tr>");
        }

        var totalTaxable = data.Sum(x => x.LineTotal);
        var totalTax = data.Sum(x => x.TaxAmount);

        var placeholders = new Dictionary<string, string>
        {
            { "{{COMPANY_NAME}}", System.Web.HttpUtility.HtmlEncode(companyName) },
            { "{{FROM_DATE}}", from.ToString("dd-MMM-yyyy") },
            { "{{TO_DATE}}", to.ToString("dd-MMM-yyyy") },
            { "{{TABLE_ROWS}}", sb.ToString() },
            { "{{TOTAL_TAXABLE}}", totalTaxable.ToString("N2") },
            { "{{TOTAL_TAX}}", totalTax.ToString("N2") },
            { "{{GENERATED_DATE}}", DateTime.Now.ToString("dd-MMM-yyyy HH:mm") }
        };

        var html = template;
        foreach (var kvp in placeholders)
        {
            html = html.Replace(kvp.Key, kvp.Value);
        }

        return html;
    }
}