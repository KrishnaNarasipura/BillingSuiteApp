using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Domain;
using Microsoft.AspNetCore.Mvc;

namespace BillingSuite.Web.Controllers;

public class InvoicesController : Controller
{
    private readonly IInvoiceService _svc;
    private readonly IVendorService _vendors;
    private readonly ITaxSettingsService _taxSettings;

    public InvoicesController(IInvoiceService svc, IVendorService vendors, ITaxSettingsService taxSettings)
    {
        _svc = svc;
        _vendors = vendors;
        _taxSettings = taxSettings;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? vendorId, int page = 1, int pageSize = 20)
    {
        // Get vendors for the dropdown filter
        ViewBag.Vendors = (await _vendors.GetVendorsAsync(null, 1, 500)).Items;

        return View(await _svc.SearchAsync(from, to, vendorId, page, pageSize));
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
        ViewBag.Vendors = (await _vendors.GetVendorsAsync(null, 1, 500)).Items;
        ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
        return View(new InvoiceCreateDto { Items = new List<InvoiceItemDto> { new() { Description = "Item 1", Quantity = 1, UnitPrice = 0 } } });
    }

    [HttpPost]
    public async Task<IActionResult> Create(InvoiceCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Vendors = (await _vendors.GetVendorsAsync(null, 1, 500)).Items;
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
            VendorId = invoice.Vendor.Id,
            InvoiceDate = invoice.InvoiceDate,
            DiscountAmount = invoice.DiscountAmount,
            Items = invoice.Items
        };

        ViewBag.Vendors = (await _vendors.GetVendorsAsync(null, 1, 500)).Items;
        ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
        return View(editDto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(InvoiceEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Vendors = (await _vendors.GetVendorsAsync(null, 1, 500)).Items;
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
}