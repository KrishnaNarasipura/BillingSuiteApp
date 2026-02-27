using Azure;
using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;
using BillingSuite.Domain.Entities;
using BillingSuite.Domain.Enums;
using BillingSuite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static QuestPDF.Helpers.Colors;

namespace BillingSuite.Infrastructure.Services;

public class TaxSettingsService : ITaxSettingsService
{
    private readonly BillingDbContext _db;
    public TaxSettingsService(BillingDbContext db) => _db = db;

    public async Task<PagedResult<TaxSettingsDto>> GetAsync(CancellationToken ct = default)
    {
        var q = _db.TaxSettings
           .AsQueryable();

        var items = await q.OrderByDescending(i => i.Id)
            .Select(txs => new TaxSettingsDto
            {
                Id = txs.Id,
                TaxPercent = txs.TaxPercent,
                TaxType = txs.TaxType,
                UpdatedOn = txs.UpdatedOn
            })
            .ToListAsync(ct);

        return new PagedResult<TaxSettingsDto> { Items = items, TotalCount = items.Count, Page = 1, PageSize = 1 };
    }

    public async Task UpdateAsync(TaxSettings settings, CancellationToken ct = default)
    {
        var existing = await _db.TaxSettings.FindAsync(settings.Id);
        if (existing is null)
            _db.TaxSettings.Add(settings);
        else
        {
            existing.TaxPercent = settings.TaxPercent;
            existing.TaxType = settings.TaxType;
            existing.UpdatedOn = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
    public async Task<int> CreateAsync(TaxSettings settings, CancellationToken ct = default)
    {
        var entity = new TaxSettings
        {
            TaxPercent = settings.TaxPercent,
            TaxType = settings.TaxType,
            UpdatedOn = DateTime.UtcNow,
        };
        _db.TaxSettings.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.TaxSettings.FindAsync([id], ct);
        if (entity is null) return;
        _db.TaxSettings.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}