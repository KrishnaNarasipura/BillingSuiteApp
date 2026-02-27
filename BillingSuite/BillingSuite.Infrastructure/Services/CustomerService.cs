using BillingSuite.Application;
using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;
using BillingSuite.Domain.Entities;
using BillingSuite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingSuite.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly BillingDbContext _db;
    public CustomerService(BillingDbContext db) => _db = db;

    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(v => v.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new CustomerDto
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

        return new PagedResult<CustomerDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Customers.Where(v => v.Id == id)
            .Select(v => new CustomerDto
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

    public async Task<int> CreateAsync(CustomerDto dto, CancellationToken ct = default)
    {
        var entity = new Customer
        {
            Name = dto.Name,
            BillingAddress = dto.BillingAddress,
            ShippingAddress = dto.ShippingAddress,
            Email = dto.Email,
            Phone = dto.Phone,
            Gstin = dto.Gstin
        };
        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(int id, CustomerDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Customers.FindAsync([id], ct) ?? throw new KeyNotFoundException("Customer not found");
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
        var entity = await _db.Customers.FindAsync([id], ct);
        if (entity is null) return;
        _db.Customers.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}