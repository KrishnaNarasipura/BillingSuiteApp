// Controllers/ReportsController.cs
using BillingSuite.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace BillingSuite.Web.Controllers;

public class ReportsController : Controller
{
    private readonly IReportService _reports;
    public ReportsController(IReportService reports) => _reports = reports;

    public IActionResult Sales() => View();

    [HttpPost]
    public async Task<IActionResult> Sales(DateTime from, DateTime to, int? vendorId)
    {
        var pdf = await _reports.SalesSummaryPdfAsync(from, to, vendorId);
        return File(pdf, "application/pdf", $"sales-{from:yyyyMMdd}-{to:yyyyMMdd}.pdf");
    }
}