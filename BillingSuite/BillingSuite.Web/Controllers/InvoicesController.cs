using BillingSuite.Application.Abstractions;
using BillingSuite.Application.Configuration;
using BillingSuite.Application.DTOs;
using BillingSuite.Application.Enums;
using BillingSuite.Domain;
using BillingSuite.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BillingSuite.Web.Controllers;

public class InvoicesController : Controller
{
    private readonly IInvoiceService _svc;
    private readonly ICustomerService _customers;
    private readonly ITaxSettingsService _taxSettings;
    private readonly BillingDbContext _db;
    private readonly InvoiceSettings _invoiceSettings;

    public InvoicesController(IInvoiceService svc, ICustomerService customers, ITaxSettingsService taxSettings, BillingDbContext db, IOptions<InvoiceSettings> invoiceSettings)
    {
        _svc = svc;
        _customers = customers;
        _taxSettings = taxSettings;
        _db = db;
        _invoiceSettings = invoiceSettings.Value;
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
        ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;
        return View(dto);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
        ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
        ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;
        return View(new InvoiceCreateDto { Items = new List<InvoiceItemDto> { new() { Description = "Item 1", Quantity = 1, UnitPrice = 0 } } });
    }

    [HttpPost]
    public async Task<IActionResult> Create(InvoiceCreateDto dto, string? submitButton)
    {
        // Clear model state for dynamic items to avoid validation issues
        // when items are added/removed dynamically via JavaScript
        if (ModelState.ContainsKey("Items"))
        {
            ModelState.Remove("Items");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;
            return View(dto);
        }

        try
        {
            int id;

            // Check which button was clicked
            if (submitButton == "SaveDraft")
            {
                // Save as draft with D- prefix
                id = await _svc.CreateDraftAsync(dto);
            }
            else
            {
                // Save as issued invoice (default Generate button)
                id = await _svc.CreateAsync(dto);
            }

            // If saved as draft, redirect to invoice list; otherwise to preview
            if (submitButton == "SaveDraft")
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return RedirectToAction(nameof(Preview), new { id });
            }
        }
        catch (Exception ex)
        {
            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;
            ModelState.AddModelError("", $"Error saving invoice: {ex.Message}");
            return View(dto);
        }
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
            OurOrderReference = invoice.OurOrderReference,
            YourOrderReference = invoice.YourOrderReference,
            CustomerId = invoice.Customer.Id,
            InvoiceDate = invoice.InvoiceDate,
            AdvanceReceived = invoice.AdvanceReceived,
            DiscountAmount = invoice.DiscountAmount,
            Items = invoice.Items
        };

        ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
        ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
        ViewBag.IsDraft = invoice.Status == 0; // Draft status is 0
        ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;
        
        return View(editDto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(InvoiceEditDto dto, string? submitButton)
    {
        // Clear model state for dynamic items to avoid validation issues
        // when items are added/removed dynamically via JavaScript
        if (ModelState.ContainsKey("Items"))
        {
            ModelState.Remove("Items");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;
            return View(dto);
        }

        try
        {
            // Check which button was clicked
            if (submitButton == "UpdateAndGenerate")
            {
                // Generate new invoice number for this draft (same logic as CreateAsync)
                var datePrefix = DateTime.UtcNow.ToString("yyyyMM");
                var countThisMonth = await _db.Invoices.CountAsync(i => 
                    i.InvoiceDate.Year == DateTime.UtcNow.Year && 
                    i.InvoiceDate.Month == DateTime.UtcNow.Month &&
                    (int)i.Status != 0); // Status != Draft (0)
                var newInvoiceNumber = $"{datePrefix}-{countThisMonth + 1:0000}";
                
                dto.InvoiceNumber = newInvoiceNumber;
                
            }

            await _svc.UpdateAsync(dto);

            // If UpdateAndGenerate, also update status to Issued
            if (submitButton == "UpdateAndGenerate")
            {
                await _svc.UpdateStatusAsync(new InvoiceUpdateStatusDto 
                { 
                    Id = dto.Id, 
                    InvoiceStatus = InvoiceStatus.Issued
                });
                return RedirectToAction(nameof(Preview), new { id = dto.Id });
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;
            ModelState.AddModelError("", $"Error updating invoice: {ex.Message}");
            return View(dto);
        }
    }

    public async Task<IActionResult> Print(int id, string invoiceNumber)
    {
        var pdf = await _svc.GeneratePdfAsync(id);
        return File(pdf, "application/pdf", $"invoice-{invoiceNumber}.pdf");
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

    [HttpPost]
    public async Task<IActionResult> AddPayment([FromBody] InvoicePaymentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        try
        {
            await _svc.AddPaymentAsync(dto);
            return Json(new { success = true, message = "Payment added successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromRoute] int id) 
    {
        try
        {
            await _svc.DeleteAsync(id);
            return Json(new { success = true, message = "Invoice deleted successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }
}