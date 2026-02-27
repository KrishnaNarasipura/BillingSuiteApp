// Services/VendorService.cs
using BillingSuite.Application;
using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;
using BillingSuite.Domain.Entities;
using BillingSuite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingSuite.Infrastructure.Services;

public class VendorService : IVendorService
{
    private readonly BillingDbContext _db;
    public VendorService(BillingDbContext db) => _db = db;

    public async Task<PagedResult<VendorDto>> GetVendorsAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Vendors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(v => v.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VendorDto
            {
                Id = v.Id,
                Name = v.Name,
                BillingAddress = v.BillingAddress,
                ShippingAddress = v.ShippingAddress,
                Email = v.Email,
                Phone = v.Phone,
                Gstin = v.Gstin
            })
            .ToListAsync(ct);

        return new PagedResult<VendorDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public Task<VendorDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Vendors.Where(v => v.Id == id)
            .Select(v => new VendorDto
            {
                Id = v.Id,
                Name = v.Name,
                BillingAddress = v.BillingAddress,
                ShippingAddress = v.ShippingAddress,
                Email = v.Email,
                Phone = v.Phone,
                Gstin = v.Gstin
            })
            .FirstOrDefaultAsync(ct)!;

    public async Task<int> CreateAsync(VendorDto dto, CancellationToken ct = default)
    {
        var entity = new Vendor
        {
            Name = dto.Name,
            BillingAddress = dto.BillingAddress,
            ShippingAddress = dto.ShippingAddress,
            Email = dto.Email,
            Phone = dto.Phone,
            Gstin = dto.Gstin
        };
        _db.Vendors.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(int id, VendorDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Vendors.FindAsync([id], ct) ?? throw new KeyNotFoundException("Vendor not found");
        entity.Name = dto.Name;
        entity.BillingAddress = dto.BillingAddress;
        entity.ShippingAddress = dto.ShippingAddress;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.Gstin = dto.Gstin;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Vendors.FindAsync([id], ct);
        if (entity is null) return;
        _db.Vendors.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}