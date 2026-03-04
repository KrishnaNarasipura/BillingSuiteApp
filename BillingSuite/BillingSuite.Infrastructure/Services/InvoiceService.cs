using BillingSuite.Application;
using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;
using BillingSuite.Domain.Entities;
using BillingSuite.Domain.Enums;
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
            CustomerId = dto.CustomerId,
            InvoiceDate = dto.InvoiceDate,
            InvoiceNumber = invoiceNumber,
            OurOrderReference = dto.OurOrderReference,
            YourOrderReference = dto.YourOrderReference,
            Subtotal = subtotal,
            TaxAmount = totalTax,
            DiscountAmount = dto.DiscountAmount,
            AdvanceReceived = dto.AdvanceReceived,
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

        existing.CustomerId = dto.CustomerId;
        existing.InvoiceNumber = dto.InvoiceNumber;
        existing.OurOrderReference = dto.OurOrderReference;
        existing.YourOrderReference = dto.YourOrderReference;
        existing.InvoiceDate = dto.InvoiceDate;
        existing.Subtotal = subtotal;
        existing.TaxAmount = totalTax;
        existing.DiscountAmount = dto.DiscountAmount;
        existing.AdvanceReceived = dto.AdvanceReceived;
        existing.NetAmount = net;

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(InvoiceUpdateStatusDto dto, CancellationToken ct = default)
    {
        var existing = await _db.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == dto.Id, ct);

        if (existing is null) return;
        existing.Status = (InvoiceStatus)dto.InvoiceStatus;
      

        await _db.SaveChangesAsync(ct);
    }

    public async Task AddPaymentAsync(InvoicePaymentDto dto, CancellationToken ct = default)
    {
        var existing = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == dto.Id, ct);

        if (existing is null) return;

        // Create a new payment record
        var payment = new InvoicePayment
        {
            InvoiceId = dto.Id,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate,
            CreatedAt = DateTime.UtcNow
        };

        _db.InvoicePayments.Add(payment);

        // Add the payment to advance received
        var newAdvanceReceived = existing.AdvanceReceived + dto.Amount;
        existing.AdvanceReceived = newAdvanceReceived;

        // Determine the status based on payment
        if (newAdvanceReceived >= existing.NetAmount)
        {
            existing.Status = InvoiceStatus.Paid;
        }
        else if (newAdvanceReceived > 0)
        {
            existing.Status = InvoiceStatus.PartiallyPaid;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<InvoiceDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var inv = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
                .ThenInclude(item => item.TaxSettings)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (inv is null) return null;

        return new InvoiceDto
        {
            Id = inv.Id,
            InvoiceNumber = inv.InvoiceNumber,
            OurOrderReference = inv.OurOrderReference,
            YourOrderReference = inv.YourOrderReference,
            InvoiceDate = inv.InvoiceDate,
            Customer = new CustomerDto
            {
                Id = inv.Customer.Id,
                Name = inv.Customer.Name,
                BillingAddress = inv.Customer.BillingAddress,
                ShippingAddress = inv.Customer.ShippingAddress,
                Email = inv.Customer.Email,
                Phone = inv.Customer.Phone,
                Gstin = inv.Customer.Gstin
            },
            Subtotal = inv.Subtotal,
            TaxAmount = inv.TaxAmount,
            DiscountAmount = inv.DiscountAmount,
            AdvanceReceived = inv.AdvanceReceived,
            NetAmount = inv.NetAmount,
            Status = (int)inv.Status,
            Items = inv.Items.Select(x => new InvoiceItemDto
            {
                Description = x.Description,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal,
                TaxSettingsId = x.TaxSettingsId,
                TaxAmount = x.TaxAmount
            }).ToList(),
            Payments = inv.Payments.OrderByDescending(p => p.PaymentDate).Select(p => new PaymentHistoryDto
            {
                Id = p.Id,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                CreatedAt = p.CreatedAt
            }).ToList()
        };
    }

    public async Task<PagedResult<InvoiceDto>> SearchAsync(DateTime? from, DateTime? to, int? CustomerId, string? invoiceNumber, int? status, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Invoices
            .Include(i => i.Customer)
            .AsQueryable();

        if (from.HasValue) q = q.Where(i => i.InvoiceDate >= from.Value);
        if (to.HasValue) q = q.Where(i => i.InvoiceDate <= to.Value);
        if (CustomerId.HasValue) q = q.Where(i => i.CustomerId == CustomerId);
        if (!string.IsNullOrWhiteSpace(invoiceNumber)) q = q.Where(i => i.InvoiceNumber == invoiceNumber);
        if (status.HasValue) q = q.Where(i => (int)i.Status == status.Value);

        var total = await q.CountAsync(ct);

        var items = await q.OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(inv => new InvoiceDto
            {
                Id = inv.Id,
                InvoiceNumber = inv.InvoiceNumber,
                OurOrderReference = inv.OurOrderReference,
                YourOrderReference = inv.YourOrderReference,
                InvoiceDate = inv.InvoiceDate,
                Customer = new CustomerDto { Id = inv.Customer.Id, Name = inv.Customer.Name },
                Subtotal = inv.Subtotal,
                TaxAmount = inv.TaxAmount,
                DiscountAmount = inv.DiscountAmount,
                AdvanceReceived = inv.AdvanceReceived,
                NetAmount = inv.NetAmount,
                Status = (int)inv.Status
            })
            .OrderByDescending(x=>x.InvoiceDate)
            .ThenBy(x=>x.InvoiceNumber)
            .ToListAsync(ct);

        return new PagedResult<InvoiceDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<byte[]> GeneratePdfAsync(int id, CancellationToken ct = default)
    {
        var inv = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
                .ThenInclude(item => item.TaxSettings)
            .FirstOrDefaultAsync(i => i.Id == id, ct) ?? throw new KeyNotFoundException("Invoice not found");

        var settings = await _db.CompanySettings.FirstOrDefaultAsync(ct) ?? new CompanySettings { CompanyName = "My Company" };

        var doc = new InvoicePdf(settings, inv);
        return doc.Render();
    }

    public async Task<List<InvoiceDto>> GetOverdueInvoicesAsync(int daysOverdue = 30, CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOverdue);

        var invoices = await _db.Invoices
            .Include(i => i.Customer)
            .Where(i => i.Status == InvoiceStatus.Issued && i.InvoiceDate <= cutoffDate)
            .OrderBy(i => i.InvoiceDate)
            .Select(inv => new InvoiceDto
            {
                Id = inv.Id,
                InvoiceNumber = inv.InvoiceNumber,
                OurOrderReference = inv.OurOrderReference,
                YourOrderReference = inv.YourOrderReference,
                InvoiceDate = inv.InvoiceDate,
                Customer = new CustomerDto { Id = inv.Customer.Id, Name = inv.Customer.Name },
                Subtotal = inv.Subtotal,
                TaxAmount = inv.TaxAmount,
                DiscountAmount = inv.DiscountAmount,
                AdvanceReceived = inv.AdvanceReceived,
                NetAmount = inv.NetAmount,
                Status = (int)inv.Status
            })
            .ToListAsync(ct);

        return invoices;
    }

    public async Task<int> CreateDraftAsync(InvoiceCreateDto dto, CancellationToken ct = default)
    {
        // Draft invoice number generator D-#### (sequential)
        var draftCount = await _db.Invoices.Where(i => i.Status == InvoiceStatus.Draft).CountAsync(ct);
        var invoiceNumber = $"D-{draftCount + 1:0000}";

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
            CustomerId = dto.CustomerId,
            InvoiceDate = dto.InvoiceDate,
            InvoiceNumber = invoiceNumber,
            OurOrderReference = dto.OurOrderReference,
            YourOrderReference = dto.YourOrderReference,
            Subtotal = subtotal,
            TaxAmount = totalTax,
            DiscountAmount = dto.DiscountAmount,
            AdvanceReceived = dto.AdvanceReceived,
            NetAmount = net,
            Status = InvoiceStatus.Draft,
            Items = items
        };

        _db.Invoices.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (existing is null) return;

        // Only allow deletion of draft invoices
        if (existing.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException("Only draft invoices can be deleted.");
        }

        // Remove payments first
        _db.InvoicePayments.RemoveRange(existing.Payments);

        // Remove items
        _db.InvoiceItems.RemoveRange(existing.Items);

        // Remove the invoice
        _db.Invoices.Remove(existing);

        await _db.SaveChangesAsync(ct);
    }
}