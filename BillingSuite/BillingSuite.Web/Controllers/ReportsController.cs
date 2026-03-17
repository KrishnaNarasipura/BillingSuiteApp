// Controllers/ReportsController.cs
using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BillingSuite.Web.Controllers;

public class ReportsController : Controller
{
    private readonly IReportService _reports;
    private readonly IInvoiceService _invoices;
    
    public ReportsController(IReportService reports, IInvoiceService invoices)
    {
        _reports = reports;
        _invoices = invoices;
    }

    public async Task<IActionResult> Index(string reportType, DateTime? from, DateTime? to, int? CustomerId)
    {
        if (!from.HasValue) from = DateTime.UtcNow.AddMonths(-1);
        if (!to.HasValue) to = DateTime.UtcNow;
        if (string.IsNullOrEmpty(reportType)) reportType = "Sales";

        ViewBag.ReportType = reportType;
        ViewBag.FromDate = from.Value;
        ViewBag.ToDate = to.Value;
        ViewBag.SelectedCustomerId = CustomerId;

        // Get customers for dropdown (used by Sales report)
        ViewBag.Customers = (await _invoices.SearchAsync(null, null, null, null, null, 1, 500)).Items
            .Select(i => i.Customer)
            .DistinctBy(c => c.Id)
            .ToList();

        if (reportType == "Tax")
        {
            var taxData = await _reports.GetTaxSummaryAsync(from.Value, to.Value);
            ViewBag.TaxData = taxData;
        }
        else
        {
            var salesData = await _reports.GetSalesSummaryAsync(from.Value, to.Value, CustomerId);
            ViewBag.SalesData = salesData;
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> PrintReport(string reportType, DateTime from, DateTime to, int? CustomerId)
    {
        string html;
        if (reportType == "Tax")
        {
            html = await _reports.TaxSummaryHtmlAsync(from, to);
        }
        else
        {
            html = await _reports.SalesSummaryHtmlAsync(from, to, CustomerId);
        }
        return Content(html, "text/html");
    }
}