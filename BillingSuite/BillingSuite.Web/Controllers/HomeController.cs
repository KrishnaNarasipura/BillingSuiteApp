using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BillingSuite.Web.Models;
using BillingSuite.Application.Abstractions;

namespace BillingSuite.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IInvoiceService _invoiceService;

    public HomeController(ILogger<HomeController> logger, IInvoiceService invoiceService)
    {
        _logger = logger;
        _invoiceService = invoiceService;
    }

    public async Task<IActionResult> Index()
    {
        // Get overdue invoices (Issued status for more than 30 days)
        var overdueInvoices = await _invoiceService.GetOverdueInvoicesAsync(30);
        ViewBag.OverdueInvoices = overdueInvoices;
        ViewBag.OverdueCount = overdueInvoices.Count;
        ViewBag.OverdueAmount = overdueInvoices.Sum(i => i.NetAmount);

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
