using BillingSuite.Application;
using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;
using BillingSuite.Domain.Entities;
using BillingSuite.Infrastructure.Persistence;
using BillingSuite.Infrastructure.Services.Pdf;
using Microsoft.EntityFrameworkCore;

namespace BillingSuite.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly BillingDbContext _db;
    public InvoiceService(BillingDbContext db) => _db = db;

    public async Task<int> CreateAsync(InvoiceCreateDto dto, CancellationToken ct = default)
    {
        // naive invoice number generator YYYYMM-#### (for demo)
        var datePrefix = dto.InvoiceDate.ToString("yyyyMM");
        var countThisMonth = await _db.Invoices.CountAsync(i => i.InvoiceDate.Year == dto.InvoiceDate.Year && i.InvoiceDate.Month == dto.InvoiceDate.Month, ct);
        var invoiceNumber = $"{datePrefix}-{countThisMonth + 1:0000}";

        var items = dto.Items.Select(i => new InvoiceItem
        {
            Description = i.Description,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = Math.Round(i.Quantity * i.UnitPrice, 2),
            TaxSettingsId = i.TaxSettingsId,
            TaxAmount = i.TaxAmount
        }).ToList();

        var subtotal = items.Sum(i => i.LineTotal);
        var totalTax = items.Sum(i => i.TaxAmount);
        var net = Math.Round(subtotal + totalTax - dto.DiscountAmount, 2);

        var entity = new Invoice
        {
            VendorId = dto.VendorId,
            InvoiceDate = dto.InvoiceDate,
            InvoiceNumber = invoiceNumber,
            Subtotal = subtotal,
            TaxAmount = totalTax,
            DiscountAmount = dto.DiscountAmount,
            NetAmount = net,
            Items = items
        };

        _db.Invoices.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(InvoiceEditDto dto, CancellationToken ct = default)
    {
        var existing = await _db.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == dto.Id, ct);

        if (existing is null) return;

        // Remove existing items
        _db.InvoiceItems.RemoveRange(existing.Items);

        // Add updated items
        var items = dto.Items.Select(i => new InvoiceItem
        {
            InvoiceId = dto.Id,
            Description = i.Description,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = Math.Round(i.Quantity * i.UnitPrice, 2),
            TaxSettingsId = i.TaxSettingsId,
            TaxAmount = i.TaxAmount
        }).ToList();

        existing.Items = items;

        // Recalculate totals
        var subtotal = items.Sum(i => i.LineTotal);
        var totalTax = items.Sum(i => i.TaxAmount);
        var net = Math.Round(subtotal + totalTax - dto.DiscountAmount, 2);

        existing.VendorId = dto.VendorId;
        existing.InvoiceDate = dto.InvoiceDate;
        existing.Subtotal = subtotal;
        existing.TaxAmount = totalTax;
        existing.DiscountAmount = dto.DiscountAmount;
        existing.NetAmount = net;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<InvoiceDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var inv = await _db.Invoices
            .Include(i => i.Vendor)
            .Include(i => i.Items)
                .ThenInclude(item => item.TaxSettings)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (inv is null) return null;

        return new InvoiceDto
        {
            Id = inv.Id,
            InvoiceNumber = inv.InvoiceNumber,
            InvoiceDate = inv.InvoiceDate,
            Vendor = new VendorDto
            {
                Id = inv.Vendor.Id,
                Name = inv.Vendor.Name,
                BillingAddress = inv.Vendor.BillingAddress,
                ShippingAddress = inv.Vendor.ShippingAddress,
                Email = inv.Vendor.Email,
                Phone = inv.Vendor.Phone,
                Gstin = inv.Vendor.Gstin
            },
            Subtotal = inv.Subtotal,
            TaxAmount = inv.TaxAmount,
            DiscountAmount = inv.DiscountAmount,
            NetAmount = inv.NetAmount,
            Items = inv.Items.Select(x => new InvoiceItemDto
            {
                Description = x.Description,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal,
                TaxSettingsId = x.TaxSettingsId,
                TaxAmount = x.TaxAmount
            }).ToList()
        };
    }

    public async Task<PagedResult<InvoiceDto>> SearchAsync(DateTime? from, DateTime? to, int? vendorId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Invoices
            .Include(i => i.Vendor)
            .AsQueryable();

        if (from.HasValue) q = q.Where(i => i.InvoiceDate >= from.Value);
        if (to.HasValue) q = q.Where(i => i.InvoiceDate <= to.Value);
        if (vendorId.HasValue) q = q.Where(i => i.VendorId == vendorId);

        var total = await q.CountAsync(ct);

        var items = await q.OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(inv => new InvoiceDto
            {
                Id = inv.Id,
                InvoiceNumber = inv.InvoiceNumber,
                InvoiceDate = inv.InvoiceDate,
                Vendor = new VendorDto { Id = inv.Vendor.Id, Name = inv.Vendor.Name },
                Subtotal = inv.Subtotal,
                TaxAmount = inv.TaxAmount,
                DiscountAmount = inv.DiscountAmount,
                NetAmount = inv.NetAmount
            })
            .ToListAsync(ct);

        return new PagedResult<InvoiceDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<byte[]> GeneratePdfAsync(int id, CancellationToken ct = default)
    {
        var inv = await _db.Invoices
            .Include(i => i.Vendor)
            .Include(i => i.Items)
                .ThenInclude(item => item.TaxSettings)
            .FirstOrDefaultAsync(i => i.Id == id, ct) ?? throw new KeyNotFoundException("Invoice not found");

        var settings = await _db.CompanySettings.FirstOrDefaultAsync(ct) ?? new CompanySettings { CompanyName = "My Company" };

        var doc = new InvoicePdf(settings, inv);
        return doc.Render();
    }
}