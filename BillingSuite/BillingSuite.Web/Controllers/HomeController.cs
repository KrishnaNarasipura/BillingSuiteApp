using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BillingSuite.Web.Models;
using BillingSuite.Application.Abstractions;

namespace BillingSuite.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IInvoiceService _invoiceService;
    private readonly IOrderService _orderService;

    public HomeController(ILogger<HomeController> logger, IInvoiceService invoiceService, IOrderService orderService)
    {
        _logger = logger;
        _invoiceService = invoiceService;
        _orderService = orderService;
    }

    public async Task<IActionResult> Index()
    {
        // Get overdue invoices (Issued status for more than 30 days)
        var overdueInvoices = await _invoiceService.GetOverdueInvoicesAsync(30);
        ViewBag.OverdueInvoices = overdueInvoices;
        ViewBag.OverdueCount = overdueInvoices.Count;
        ViewBag.OverdueAmount = overdueInvoices.Sum(i => i.NetAmount);

        // Get financial year statistics for completed orders and paid invoices
        var completedOrdersStats = await _orderService.GetCompletedOrdersStatsForFinancialYearAsync();
        var paidInvoicesStats = await _invoiceService.GetPaidInvoicesStatsForFinancialYearAsync();
        
        ViewBag.CompletedOrdersCount = completedOrdersStats.TotalCount;
        ViewBag.CompletedOrdersAmount = completedOrdersStats.TotalAmount;
        ViewBag.PaidInvoicesCount = paidInvoicesStats.TotalCount;
        ViewBag.PaidInvoicesAmount = paidInvoicesStats.TotalAmount;

        // Get financial year statistics for confirmed orders and issued invoices
        var confirmedOrdersStats = await _orderService.GetConfirmedOrdersStatsForFinancialYearAsync();
        var issuedInvoicesStats = await _invoiceService.GetIssuedInvoicesStatsForFinancialYearAsync();
        
        ViewBag.ConfirmedOrdersCount = confirmedOrdersStats.TotalCount;
        ViewBag.ConfirmedOrdersAmount = confirmedOrdersStats.TotalAmount;
        ViewBag.IssuedInvoicesCount = issuedInvoicesStats.TotalCount;
        ViewBag.IssuedInvoicesAmount = issuedInvoicesStats.TotalAmount;

        // Calculate current financial year for display
        var currentDate = DateTime.UtcNow;
        var fyStartYear = currentDate.Month >= 4 ? currentDate.Year : currentDate.Year - 1;
        ViewBag.FinancialYear = $"{fyStartYear}-{fyStartYear + 1}";

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
