// Controllers/SettingsController.cs
using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Domain.Entities;
using BillingSuite.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BillingSuite.Web.Controllers;

public class SettingsController : Controller
{
    private readonly ICompanySettingsService _companySettingsService;
    private readonly ITaxSettingsService _taxSettingsService;
    
    public SettingsController(ICompanySettingsService companySettingsService, ITaxSettingsService taxSettingsService)
    {
        _companySettingsService = companySettingsService;
        _taxSettingsService = taxSettingsService;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new SettingsViewModel
        {
            CompanySettings = await _companySettingsService.GetAsync(),
            TaxSettings = await _taxSettingsService.GetAsync()
        };
        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Index(SettingsViewModel model, IFormFile? logo)
    {
        if (logo is not null && logo.Length > 0)
        {
            using var ms = new MemoryStream();
            await logo.CopyToAsync(ms);
            model.CompanySettings.LogoBytes = ms.ToArray();
        }
        
        await _companySettingsService.UpdateAsync(model.CompanySettings);
        
        TempData["msg"] = "Company settings saved";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTaxSetting(TaxSettingsDto taxSetting)
    {
        if (ModelState.IsValid)
        {
            
            await _taxSettingsService.UpdateAsync(MapDto(taxSetting));
            TempData["msg"] = "Tax setting updated successfully";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AddTaxSetting(TaxSettingsDto newTaxSetting)
    {
        if (ModelState.IsValid)
        {
            await _taxSettingsService.CreateAsync(MapDto(newTaxSetting));
            TempData["msg"] = "Tax setting added successfully";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTaxSetting(int id)
    {
        await _taxSettingsService.DeleteAsync(id);
        TempData["msg"] = "Tax setting deleted successfully";
        return RedirectToAction(nameof(Index));
    }

    private TaxSettings MapDto(TaxSettingsDto model)
    {
        return new TaxSettings
        {
            Id = model.Id,
            TaxPercent = model.TaxPercent,
            TaxType = model.TaxType,
            UpdatedOn = model.UpdatedOn
        };
    }
}