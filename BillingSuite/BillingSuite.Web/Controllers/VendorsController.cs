// Controllers/VendorsController.cs
using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BillingSuite.Web.Controllers;

public class VendorsController : Controller
{
    private readonly IVendorService _svc;
    public VendorsController(IVendorService svc) => _svc = svc;

    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 20)
        => View(await _svc.GetVendorsAsync(q, page, pageSize));

    public IActionResult Create() => View(new VendorDto());

    [HttpPost]
    public async Task<IActionResult> Create(VendorDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _svc.CreateAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _svc.GetByIdAsync(id);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, VendorDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _svc.UpdateAsync(id, dto);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}