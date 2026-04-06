using BillingSuite.Application.Abstractions;
using BillingSuite.Application.Configuration;
using BillingSuite.Application.DTOs;
using BillingSuite.Application.Enums;
using BillingSuite.Domain;
using BillingSuite.Domain.Enums;
using BillingSuite.Infrastructure.Logging;
using BillingSuite.Infrastructure.Persistence;
using BillingSuite.Infrastructure.Services;
using BillingSuite.Infrastructure.Services.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using InvoiceStatus = BillingSuite.Application.Enums.InvoiceStatus;

namespace BillingSuite.Web.Controllers;

public class InvoicesController : Controller
{
    private readonly IInvoiceService _svc;
    private readonly ICustomerService _customers;
    private readonly ITaxSettingsService _taxSettings;
    private readonly BillingDbContext _db;
    private readonly InvoiceSettings _invoiceSettings;
    private readonly ILogger<InvoicesController> _logger;
    private readonly IPrintHistoryService _printHistoryService;
    private readonly PdfSaveService _pdfSaveService;
    private readonly HtmlToPdfService _htmlToPdfService;

    public InvoicesController(
        IInvoiceService svc,
        ICustomerService customers,
        ITaxSettingsService taxSettings,
        BillingDbContext db,
        IOptions<InvoiceSettings> invoiceSettings,
        ILogger<InvoicesController> logger,
        IPrintHistoryService printHistoryService,
        PdfSaveService pdfSaveService,
        HtmlToPdfService htmlToPdfService)
    {
        _svc = svc;
        _customers = customers;
        _taxSettings = taxSettings;
        _db = db;
        _invoiceSettings = invoiceSettings.Value;
        _logger = logger;
        _printHistoryService = printHistoryService;
        _pdfSaveService = pdfSaveService;
        _htmlToPdfService = htmlToPdfService;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? CustomerId, string? invoiceNumber, int? status, int page = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogUserAction("Accessed Invoices Index", data: new { from, to, CustomerId, invoiceNumber, status, page, pageSize });

            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            var result = await _svc.SearchAsync(from, to, CustomerId, invoiceNumber, status, page, pageSize);

            _logger.LogDebug("Invoices search returned {Count} results", result.Items.Count);

            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Index - Search Invoices", ex, new { from, to, CustomerId, invoiceNumber, status });
            return View(new BillingSuite.Application.Results.PagedResult<InvoiceDto>());
        }
    }

    public async Task<IActionResult> Preview(int id)
    {
        try
        {
            _logger.LogUserAction("Accessed Invoice Preview", data: new { InvoiceId = id });

            var dto = await _svc.GetAsync(id);
            if (dto is null)
            {
                _logger.LogWarning("Invoice not found: {InvoiceId}", id);
                return NotFound();
            }

            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;

            _logger.LogDebug("Invoice preview loaded successfully for {InvoiceId}", id);

            return View(dto);
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Preview Invoice", ex, new { InvoiceId = id });
            return NotFound();
        }
    }

    public async Task<IActionResult> Create()
    {
        try
        {
            _logger.LogUserAction("Accessed Invoice Create Form");

            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;

            return View(new InvoiceCreateDto { Items = new List<InvoiceItemDto> { new() { Description = "Item 1", Quantity = 1, UnitPrice = 0 } } });
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Create Invoice - Get Form", ex);
            return View(new InvoiceCreateDto { Items = new List<InvoiceItemDto> { new() { Description = "Item 1", Quantity = 1, UnitPrice = 0 } } });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(InvoiceCreateDto dto, string? submitButton)
    {
        try
        {
            _logger.LogDebug("Invoice create form submitted with button: {SubmitButton}", submitButton);

            // Clear model state for dynamic items to avoid validation issues
            if (ModelState.ContainsKey("Items"))
            {
                ModelState.Remove("Items");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogValidationError("Invoice", "Invalid model state", new { CustomerId = dto.CustomerId, ItemCount = dto.Items?.Count ?? 0 });

                ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
                ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
                ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;
                return View(dto);
            }

            int id;

            // Check which button was clicked
            if (submitButton == "SaveDraft")
            {
                id = await _svc.CreateDraftAsync(dto);
                _logger.LogBusinessEvent("Invoice Draft Created", new { InvoiceId = id, CustomerId = dto.CustomerId, Amount = dto.Items?.Sum(x => x.LineTotal) ?? 0 });
            }
            else
            {
                id = await _svc.CreateAsync(dto);
                _logger.LogBusinessEvent("Invoice Created", new { InvoiceId = id, CustomerId = dto.CustomerId, Amount = dto.Items?.Sum(x => x.LineTotal) ?? 0 });
            }

            // If saved as draft, redirect to invoice list; otherwise to preview
            if (submitButton == "SaveDraft")
            {
                _logger.LogInformation("Redirecting to Invoice Index after draft save");
                return RedirectToAction(nameof(Index));
            }
            else
            {
                _logger.LogInformation("Redirecting to Invoice Preview for ID: {InvoiceId}", id);
                return RedirectToAction(nameof(Preview), new { id });
            }
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Create Invoice", ex, new { CustomerId = dto.CustomerId, ItemCount = dto.Items?.Count ?? 0, SubmitButton = submitButton });

            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;
            ModelState.AddModelError("", $"Error saving invoice: {ex.Message}");
            return View(dto);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            _logger.LogUserAction("Accessed Invoice Edit", data: new { InvoiceId = id });

            var invoice = await _svc.GetAsync(id);
            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for edit: {InvoiceId}", id);
                return NotFound();
            }

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
            ViewBag.IsDraft = invoice.Status == 0;
            ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;

            _logger.LogDebug("Invoice edit form loaded for {InvoiceId}, Status: {Status}", id, invoice.Status);

            return View(editDto);
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Edit Invoice - Get Form", ex, new { InvoiceId = id });
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(InvoiceEditDto dto, string? submitButton)
    {
        try
        {
            _logger.LogDebug("Invoice edit form submitted with button: {SubmitButton}", submitButton);

            // Clear model state for dynamic items
            if (ModelState.ContainsKey("Items"))
            {
                ModelState.Remove("Items");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogValidationError("Invoice Edit", "Invalid model state", new { InvoiceId = dto.Id, ItemCount = dto.Items?.Count ?? 0 });

                ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
                ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
                ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;
                return View(dto);
            }

            try
            {
                if (submitButton == "UpdateAndGenerate")
                {
                    var datePrefix = DateTime.UtcNow.ToString("yyyyMM");
                    var countThisMonth = await _db.Invoices.CountAsync(i =>
                        i.InvoiceDate.Year == DateTime.UtcNow.Year &&
                        i.InvoiceDate.Month == DateTime.UtcNow.Month &&
                        (int)i.Status != 0);
                    var newInvoiceNumber = $"{datePrefix}-{countThisMonth + 1:0000}";

                    dto.InvoiceNumber = newInvoiceNumber;
                    _logger.LogDebug("Generated new invoice number: {InvoiceNumber}", newInvoiceNumber);
                }

                await _svc.UpdateAsync(dto);
                _logger.LogBusinessEvent("Invoice Updated", new { InvoiceId = dto.Id, InvoiceNumber = dto.InvoiceNumber, SubmitButton = submitButton });

                if (submitButton == "UpdateAndGenerate")
                {
                    await _svc.UpdateStatusAsync(new InvoiceUpdateStatusDto
                    {
                        Id = dto.Id,
                        InvoiceStatus = InvoiceStatus.Issued
                    });
                    _logger.LogBusinessEvent("Invoice Status Changed", new { InvoiceId = dto.Id, NewStatus = "Issued" });

                    return RedirectToAction(nameof(Preview), new { id = dto.Id });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogOperationError("Update Invoice", ex, new { InvoiceId = dto.Id, SubmitButton = submitButton });
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Edit Invoice", ex, new { InvoiceId = dto.Id, ItemCount = dto.Items?.Count ?? 0 });

            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            ViewBag.ShowDiscountAndAdvance = _invoiceSettings.ShowDiscountAndAdvance;
            ModelState.AddModelError("", $"Error updating invoice: {ex.Message}");
            return View(dto);
        }
    }

    public async Task<IActionResult> PrintPreview(int id)
    {
        try
        {
            _logger.LogUserAction("Accessed Invoice Print Preview", data: new { InvoiceId = id });

            var inv = await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                    .ThenInclude(item => item.TaxSettings)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inv is null)
            {
                _logger.LogWarning("Invoice not found for print preview: {InvoiceId}", id);
                return NotFound();
            }

            var settings = await _db.CompanySettings.FirstOrDefaultAsync() ?? new BillingSuite.Domain.Entities.CompanySettings { CompanyName = "My Company" };

            var htmlInvoice = new InvoiceHtml(settings, inv);
            var htmlContent = htmlInvoice.Render();

            _logger.LogDebug("Invoice HTML generated successfully for {InvoiceId}", id);

            return Content(htmlContent, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Print Preview Invoice", ex, new { InvoiceId = id });
            return NotFound();
        }
    }

    public async Task<IActionResult> Print(int id, string invoiceNumber)
    {
        try
        {
            _logger.LogUserAction("Accessed Invoice PDF Generation", 
                data: new { InvoiceId = id, InvoiceNumber = invoiceNumber });

            // Get invoice data (same as PrintPreview)
            var inv = await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                    .ThenInclude(item => item.TaxSettings)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inv is null)
            {
                _logger.LogWarning("Invoice not found: {InvoiceId}", id);
                return NotFound();
            }

            // Get company settings
            var settings = await _db.CompanySettings.FirstOrDefaultAsync() 
                ?? new BillingSuite.Domain.Entities.CompanySettings { CompanyName = "My Company" };

            // Generate HTML using existing InvoiceHtml class (same as PrintPreview)
            var htmlInvoice = new InvoiceHtml(settings, inv);
            var singlePageHtml = htmlInvoice.Render();

            // Create multi-page HTML with two copies: Original and Office Copy
            var multiPageHtml = CreateMultiPageInvoiceHtml(singlePageHtml);

            _logger.LogDebug("Multi-page invoice HTML generated successfully for {InvoiceId}", id);

            // Convert HTML to PDF using HtmlToPdfService
            var pdfBytes = _htmlToPdfService.ConvertHtmlToPdf(multiPageHtml, invoiceNumber);

             _logger.LogDebug("PDF conversion completed, size: {SizeKB}KB", pdfBytes.Length / 1024);

            // Save PDF to disk
            var pdfPath = await _pdfSaveService.SavePdfAsync(pdfBytes, invoiceNumber);

            // Record print in database
            //await _printHistoryService.RecordPrintAsync(new RecordPrintDto
            //{
            //    InvoiceId = id,
            //    InvoiceNumber = invoiceNumber,
            //    PdfPath = pdfPath,
            //    PrintedTime = DateTime.UtcNow
            //});

            _logger.LogBusinessEvent("Invoice PDF Generated and Saved", new { 
                InvoiceId = id, 
                InvoiceNumber = invoiceNumber, 
                PdfPath = pdfPath 
            });

            // Return file for download
            return File(pdfBytes, "application/pdf", $"invoice-{invoiceNumber}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Print Invoice PDF", ex, new { InvoiceId = id, InvoiceNumber = invoiceNumber });
            throw;
        }
    }

    /// <summary>
    /// Creates a PDF-optimized version by removing print buttons
    /// The template already includes two pages with labels, so we just clean up the HTML
    /// </summary>
    private string CreatePdfOptimizedHtml(string invoiceHtml)
    {
        // Remove the print buttons - don't need them in PDF
        var printButtonStartIndex = invoiceHtml.IndexOf("<div class='print-button'", StringComparison.OrdinalIgnoreCase);
        if (printButtonStartIndex >= 0)
        {
            var printButtonEndIndex = invoiceHtml.IndexOf("</div>", printButtonStartIndex, StringComparison.OrdinalIgnoreCase);
            if (printButtonEndIndex >= 0)
            {
                invoiceHtml = invoiceHtml.Substring(0, printButtonStartIndex) + 
                              invoiceHtml.Substring(printButtonEndIndex + 6);
            }
        }

        return invoiceHtml;
    }

    /// <summary>
    /// Creates a multi-page HTML document for PDF
    /// The template already includes both pages with labels, so just return the optimized HTML
    /// </summary>
    private string CreateMultiPageInvoiceHtml(string invoiceHtml)
    {
        // Just return the PDF-optimized version
        // The template already contains both pages with "ORIGINAL FOR RECIPIENT" and "FOR OFFICE COPY" labels
        return CreatePdfOptimizedHtml(invoiceHtml);
    }

    [HttpPost]
    public IActionResult ConvertNumberToWords([FromBody] decimal amount)
    {
        try
        {
            _logger.LogDebug("Converting amount to words: {Amount}", amount);

            var words = Utility.ConvertNumberToWords(amount);

            return Json(new { words });
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Convert Number to Words", ex, new { Amount = amount });
            return Json(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus([FromBody] InvoiceUpdateStatusDto dto)
    {
        try
        {
            _logger.LogDebug("Updating invoice status via API");

            if (!ModelState.IsValid)
            {
                _logger.LogValidationError("Invoice Status Update", "Invalid model state", dto);
                return Json(new { success = false, message = "Invalid data" });
            }

            await _svc.UpdateStatusAsync(dto);

            _logger.LogBusinessEvent("Invoice Status Updated", new { InvoiceId = dto.Id, NewStatus = dto.InvoiceStatus });

            return Json(new { success = true, message = "Invoice status updated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Update Invoice Status", ex, dto);
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddPayment([FromBody] InvoicePaymentDto dto)
    {
        try
        {
            _logger.LogDebug("Adding payment via API");

            if (!ModelState.IsValid)
            {
                _logger.LogValidationError("Invoice Payment", "Invalid model state", dto);
                return Json(new { success = false, message = "Invalid data" });
            }

            // Validate payment mode specific fields
            if (dto.PaymentMode == PaymentMode.Cheque && string.IsNullOrWhiteSpace(dto.ChequeNumber))
            {
                _logger.LogValidationError("Invoice Payment", "Cheque number missing for cheque payment", dto);
                return Json(new { success = false, message = "Cheque number is required for cheque payments." });
            }

            if (dto.PaymentMode == PaymentMode.Online && string.IsNullOrWhiteSpace(dto.TransactionReference))
            {
                _logger.LogValidationError("Invoice Payment", "Transaction reference missing for online payment", dto);
                return Json(new { success = false, message = "Transaction reference is required for online payments." });
            }

            await _svc.AddPaymentAsync(dto);

            _logger.LogBusinessEvent("Payment Added", new {
                InvoiceId = dto.Id,
                Amount = dto.Amount,
                PaymentMode = dto.PaymentMode
            });

            return Json(new { success = true, message = "Payment added successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Add Payment", ex, new { InvoiceId = dto.Id, Amount = dto.Amount });
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        try
        {
            _logger.LogUserAction("Requesting Invoice Delete", data: new { InvoiceId = id });

            await _svc.DeleteAsync(id);

            _logger.LogBusinessEvent("Invoice Deleted", new { InvoiceId = id });

            return Json(new { success = true, message = "Invoice deleted successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation on invoice delete: {InvoiceId}", id);
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Delete Invoice", ex, new { InvoiceId = id });
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }
}