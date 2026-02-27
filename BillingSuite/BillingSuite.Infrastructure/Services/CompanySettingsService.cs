// Services/CompanySettingsService.cs
using BillingSuite.Application.Abstractions;
using BillingSuite.Domain.Entities;
using BillingSuite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingSuite.Infrastructure.Services;

public class CompanySettingsService : ICompanySettingsService
{
    private readonly BillingDbContext _db;
    public CompanySettingsService(BillingDbContext db) => _db = db;

    public async Task<CompanySettings> GetAsync(CancellationToken ct = default) =>
        await _db.CompanySettings.FirstOrDefaultAsync(ct) ?? new CompanySettings { CompanyName = "My Company" };

    public async Task UpdateAsync(CompanySettings settings, CancellationToken ct = default)
    {
        var existing = await _db.CompanySettings.FirstOrDefaultAsync(ct);
        if (existing is null)
            _db.CompanySettings.Add(settings);
        else
        {
            existing.CompanyName = settings.CompanyName;
            existing.Address = settings.Address;
            existing.Phone = settings.Phone;
            existing.Gstin = settings.Gstin;
            existing.LogoBytes = settings.LogoBytes;
            existing.TermsAndConditions = settings.TermsAndConditions;
            existing.UpdatedOn = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
}