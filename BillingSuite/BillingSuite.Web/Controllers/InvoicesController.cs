using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Domain;
using Microsoft.AspNetCore.Mvc;

namespace BillingSuite.Web.Controllers;

public class InvoicesController : Controller
{
    private readonly IInvoiceService _svc;
    private readonly ICustomerService _customers;
    private readonly ITaxSettingsService _taxSettings;

    public InvoicesController(IInvoiceService svc, ICustomerService customers, ITaxSettingsService taxSettings)
    {
        _svc = svc;
        _customers = customers;
        _taxSettings = taxSettings;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? CustomerId, string? invoiceNumber, int? status, int page = 1, int pageSize = 20)
    {
        // Get customers for the dropdown filter
        ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;

        return View(await _svc.SearchAsync(from, to, CustomerId, invoiceNumber, status, page, pageSize));
    }

    public async Task<IActionResult> Preview(int id)
    {
        var dto = await _svc.GetAsync(id);
        if (dto is null) return NotFound();

        ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
        return View(dto);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
        ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
        return View(new InvoiceCreateDto { Items = new List<InvoiceItemDto> { new() { Description = "Item 1", Quantity = 1, UnitPrice = 0 } } });
    }

    [HttpPost]
    public async Task<IActionResult> Create(InvoiceCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            return View(dto);
        }
        var id = await _svc.CreateAsync(dto);
        return RedirectToAction(nameof(Preview), new { id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var invoice = await _svc.GetAsync(id);
        if (invoice == null) return NotFound();

        // Convert InvoiceDto to InvoiceEditDto
        var editDto = new InvoiceEditDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = invoice.Customer.Id,
            InvoiceDate = invoice.InvoiceDate,
            DiscountAmount = invoice.DiscountAmount,
            Items = invoice.Items
        };

        ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
        ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
        return View(editDto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(InvoiceEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            return View(dto);
        }

        await _svc.UpdateAsync(dto);
        return RedirectToAction(nameof(Preview), new { id = dto.Id });
    }

    public async Task<IActionResult> Print(int id)
    {
        var pdf = await _svc.GeneratePdfAsync(id);
        return File(pdf, "application/pdf", $"invoice-{id}.pdf");
    }

    [HttpPost]
    public IActionResult ConvertNumberToWords([FromBody] decimal amount)
    {
        var words = Utility.ConvertNumberToWords(amount);
        return Json(new { words });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus([FromBody] InvoiceUpdateStatusDto dto)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        try
        {
            await _svc.UpdateStatusAsync(dto);
            return Json(new { success = true, message = "Invoice status updated successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }
}