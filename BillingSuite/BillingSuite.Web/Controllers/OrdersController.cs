using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Application.Enums;
using BillingSuite.Domain;
using BillingSuite.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillingSuite.Web.Controllers;

public class OrdersController : Controller
{
    private readonly IOrderService _svc;
    private readonly ICustomerService _customers;
    private readonly ITaxSettingsService _taxSettings;
    private readonly BillingDbContext _db;

    public OrdersController(IOrderService svc, ICustomerService customers, ITaxSettingsService taxSettings, BillingDbContext db)
    {
        _svc = svc;
        _customers = customers;
        _taxSettings = taxSettings;
        _db = db;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? CustomerId, string? orderNumber, int? status, int page = 1, int pageSize = 20)
    {
        // Get customers for the dropdown filter
        ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;

        return View(await _svc.SearchAsync(from, to, CustomerId, orderNumber, status, page, pageSize));
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
        ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
        return View(new OrderCreateDto { Items = new List<OrderItemDto> { new() { Description = "Item 1", Quantity = 1, UnitPrice = 0 } } });
    }

    [HttpPost]
    public async Task<IActionResult> Create(OrderCreateDto dto, string? submitButton)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
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
                // Save as confirmed order (default Confirm button)
                id = await _svc.CreateAsync(dto);
            }

            // Redirect to index
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            ModelState.AddModelError("", $"Error saving order: {ex.Message}");
            return View(dto);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var order = await _svc.GetAsync(id);
        if (order == null) return NotFound();

        // Convert OrderDto to OrderEditDto
        var editDto = new OrderEditDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.Customer.Id,
            OrderDate = order.OrderDate,
            DiscountAmount = order.DiscountAmount,
            AdvanceReceived = order.AdvanceReceived,
            Items = order.Items
        };

        ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
        ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
        ViewBag.IsDraft = order.Status == 0; // Draft status is 0

        return View(editDto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(OrderEditDto dto, string? submitButton)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            return View(dto);
        }

        try
        {
            await _svc.UpdateAsync(dto);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ViewBag.Customers = (await _customers.GetCustomersAsync(null, 1, 500)).Items;
            ViewBag.TaxSettings = (await _taxSettings.GetAsync()).Items;
            ModelState.AddModelError("", $"Error updating order: {ex.Message}");
            return View(dto);
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus([FromBody] OrderUpdateStatusDto dto)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        try
        {
            await _svc.UpdateStatusAsync(dto);
            return Json(new { success = true, message = "Order status updated successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }
}
