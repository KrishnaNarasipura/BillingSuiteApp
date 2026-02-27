// Controllers/ReportsController.cs
using BillingSuite.Application.Abstractions;
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

    public async Task<IActionResult> Sales(DateTime? from, DateTime? to, int? CustomerId)
    {
        // Set default date range if not provided
        if (!from.HasValue) from = DateTime.UtcNow.AddMonths(-1);
        if (!to.HasValue) to = DateTime.UtcNow;

        // Get customers for dropdown
        ViewBag.Customers = (await _invoices.SearchAsync(null, null, null, null, null, 1, 500)).Items
            .Select(i => i.Customer)
            .Distinct()
            .ToList();

        // Get sales summary data
        var salesData = await _reports.GetSalesSummaryAsync(from.Value, to.Value, CustomerId);
        
        ViewBag.FromDate = from.Value;
        ViewBag.ToDate = to.Value;
        ViewBag.SelectedCustomerId = CustomerId;

        return View(salesData);
    }

    [HttpPost]
    public async Task<IActionResult> PrintSalesSummary(DateTime from, DateTime to, int? CustomerId)
    {
        var pdf = await _reports.SalesSummaryPdfAsync(from, to, CustomerId);
        return File(pdf, "application/pdf", $"sales-{from:yyyyMMdd}-{to:yyyyMMdd}.pdf");
    }
}