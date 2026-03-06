using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;
using BillingSuite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingSuite.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly BillingDbContext _db;

    public PaymentService(BillingDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PaymentViewDto>> SearchAsync(DateTime? from, DateTime? to, int? customerId, string? invoiceNumber, string? orderReference, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.InvoicePayments
            .Include(p => p.Invoice)
            .ThenInclude(i => i.Customer)
            .AsQueryable();

        // Apply filters
        if (from.HasValue)
            query = query.Where(p => p.PaymentDate >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.PaymentDate <= to.Value);

        if (customerId.HasValue)
            query = query.Where(p => p.Invoice.CustomerId == customerId.Value);

        if (!string.IsNullOrEmpty(invoiceNumber))
            query = query.Where(p => p.Invoice.InvoiceNumber.Contains(invoiceNumber));

        if (!string.IsNullOrEmpty(orderReference))
            query = query.Where(p => p.Invoice.OurOrderReference != null && p.Invoice.OurOrderReference.Contains(orderReference));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentViewDto
            {
                Id = p.Id,
                InvoiceNumber = p.Invoice.InvoiceNumber,
                CustomerName = p.Invoice.Customer.Name,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentMode = p.PaymentMode,
                ChequeNumber = p.ChequeNumber,
                TransactionReference = p.TransactionReference,
                OurOrderReference = p.Invoice.OurOrderReference,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<PaymentViewDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<PaymentViewDto>> GetPaymentsByInvoiceIdAsync(int invoiceId, CancellationToken ct = default)
    {
        return await _db.InvoicePayments
            .Include(p => p.Invoice)
            .ThenInclude(i => i.Customer)
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentViewDto
            {
                Id = p.Id,
                InvoiceNumber = p.Invoice.InvoiceNumber,
                CustomerName = p.Invoice.Customer.Name,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentMode = p.PaymentMode,
                ChequeNumber = p.ChequeNumber,
                TransactionReference = p.TransactionReference,
                OurOrderReference = p.Invoice.OurOrderReference,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<PaymentViewDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.InvoicePayments
            .Include(p => p.Invoice)
            .ThenInclude(i => i.Customer)
            .Where(p => p.Id == id)
            .Select(p => new PaymentViewDto
            {
                Id = p.Id,
                InvoiceNumber = p.Invoice.InvoiceNumber,
                CustomerName = p.Invoice.Customer.Name,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentMode = p.PaymentMode,
                ChequeNumber = p.ChequeNumber,
                TransactionReference = p.TransactionReference,
                OurOrderReference = p.Invoice.OurOrderReference,
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<decimal> GetTotalPaymentsByInvoiceIdAsync(int invoiceId, CancellationToken ct = default)
    {
        return await _db.InvoicePayments
            .Where(p => p.InvoiceId == invoiceId)
            .SumAsync(p => p.Amount, ct);
    }

    public async Task<List<PaymentViewDto>> GetRecentPaymentsAsync(int count = 10, CancellationToken ct = default)
    {
        return await _db.InvoicePayments
            .Include(p => p.Invoice)
            .ThenInclude(i => i.Customer)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .Select(p => new PaymentViewDto
            {
                Id = p.Id,
                InvoiceNumber = p.Invoice.InvoiceNumber,
                CustomerName = p.Invoice.Customer.Name,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentMode = p.PaymentMode,
                ChequeNumber = p.ChequeNumber,
                TransactionReference = p.TransactionReference,
                OurOrderReference = p.Invoice.OurOrderReference,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(PaymentEditDto dto, CancellationToken ct = default)
    {
        var existingPayment = await _db.InvoicePayments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.Id == dto.Id, ct);

        if (existingPayment == null) return;

        // Calculate the difference in payment amount to adjust invoice advance received
        var amountDifference = dto.Amount - existingPayment.Amount;

        // Update payment details
        existingPayment.Amount = dto.Amount;
        existingPayment.PaymentDate = dto.PaymentDate;
        existingPayment.PaymentMode = dto.PaymentMode;
        existingPayment.ChequeNumber = dto.ChequeNumber;
        existingPayment.TransactionReference = dto.TransactionReference;

        // Update invoice advance received amount
        existingPayment.Invoice.AdvanceReceived += amountDifference;

        // Recalculate invoice status based on new advance received amount
        if (existingPayment.Invoice.AdvanceReceived >= existingPayment.Invoice.NetAmount)
        {
            existingPayment.Invoice.Status = Domain.Enums.InvoiceStatus.Paid;
        }
        else if (existingPayment.Invoice.AdvanceReceived > 0)
        {
            existingPayment.Invoice.Status = Domain.Enums.InvoiceStatus.PartiallyPaid;
        }
        else
        {
            existingPayment.Invoice.Status = Domain.Enums.InvoiceStatus.Issued;
        }

        await _db.SaveChangesAsync(ct);
    }
}