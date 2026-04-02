using BillingSuite.Application;
using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;
using BillingSuite.Domain.Entities;
using BillingSuite.Domain.Enums;
using BillingSuite.Infrastructure.Logging;
using BillingSuite.Infrastructure.Persistence;
using BillingSuite.Infrastructure.Services.Pdf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingSuite.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly BillingDbContext _db;
    private readonly IOrderService _orderService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(BillingDbContext db, IOrderService orderService, ILogger<InvoiceService> logger)
    {
        _db = db;
        _orderService = orderService;
        _logger = logger;
    }

    public async Task<int> CreateAsync(InvoiceCreateDto dto, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Starting invoice creation for customer {CustomerId}", dto.CustomerId);
            
            var datePrefix = dto.InvoiceDate.ToString("yyyyMM");
            var countThisMonth = await _db.Invoices.CountAsync(i => i.InvoiceDate.Year == dto.InvoiceDate.Year && i.InvoiceDate.Month == dto.InvoiceDate.Month, ct);
            var invoiceNumber = $"{datePrefix}-{countThisMonth + 1:0000}";

            var items = dto.Items.Select(i => new InvoiceItem
            {
                Description = i.Description,
                HsnCode = i.HsnCode,
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

            _logger.LogBusinessEvent("Invoice Created", new { 
                InvoiceId = entity.Id, 
                InvoiceNumber = invoiceNumber, 
                CustomerId = dto.CustomerId,
                Amount = net,
                ItemCount = items.Count
            });

            if (!string.IsNullOrWhiteSpace(dto.OurOrderReference))
            {
                var relatedOrder = await _db.Orders
                    .FirstOrDefaultAsync(o => o.OrderNumber == dto.OurOrderReference, ct);

                if (relatedOrder != null)
                {
                    await _orderService.UpdateStatusAsync(new OrderUpdateStatusDto
                    {
                        Id = relatedOrder.Id,
                        OrderStatus = (int)OrderStatus.InvoiceIssued
                    }, ct);
                    
                    _logger.LogDebug("Updated order status to InvoiceIssued for order {OrderNumber}", dto.OurOrderReference);
                }
            }
            return entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Create Invoice", ex, new { CustomerId = dto.CustomerId, ItemCount = dto.Items?.Count ?? 0 });
            throw;
        }
    }

    public async Task UpdateAsync(InvoiceEditDto dto, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Updating invoice {InvoiceId}", dto.Id);
            
            var existing = await _db.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == dto.Id, ct);

            if (existing is null)
            {
                _logger.LogWarning("Invoice not found for update: {InvoiceId}", dto.Id);
                return;
            }

            _db.InvoiceItems.RemoveRange(existing.Items);

            var items = dto.Items.Select(i => new InvoiceItem
            {
                InvoiceId = dto.Id,
                Description = i.Description,
                HsnCode = i.HsnCode,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = Math.Round(i.Quantity * i.UnitPrice, 2),
                TaxSettingsId = i.TaxSettingsId,
                TaxAmount = i.TaxAmount
            }).ToList();

            existing.Items = items;

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

            _logger.LogBusinessEvent("Invoice Updated", new { 
                InvoiceId = dto.Id, 
                InvoiceNumber = dto.InvoiceNumber, 
                Amount = net 
            });

            if (!string.IsNullOrWhiteSpace(dto.OurOrderReference))
            {
                var relatedOrder = await _db.Orders
                    .FirstOrDefaultAsync(o => o.OrderNumber == dto.OurOrderReference, ct);

                if (relatedOrder != null)
                {
                    await _orderService.UpdateStatusAsync(new OrderUpdateStatusDto
                    {
                        Id = relatedOrder.Id,
                        OrderStatus = (int)OrderStatus.InvoiceIssued
                    }, ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Update Invoice", ex, new { InvoiceId = dto.Id });
            throw;
        }
    }

    public async Task UpdateStatusAsync(InvoiceUpdateStatusDto dto, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Updating invoice status for {InvoiceId} to {NewStatus}", dto.Id, dto.InvoiceStatus);
            
            var existing = await _db.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == dto.Id, ct);

            if (existing is null)
            {
                _logger.LogWarning("Invoice not found for status update: {InvoiceId}", dto.Id);
                return;
            }

            var oldStatus = existing.Status;
            existing.Status = (InvoiceStatus)dto.InvoiceStatus;

            await _db.SaveChangesAsync(ct);

            _logger.LogBusinessEvent("Invoice Status Changed", new { 
                InvoiceId = dto.Id, 
                OldStatus = oldStatus, 
                NewStatus = existing.Status 
            });
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Update Invoice Status", ex, dto);
            throw;
        }
    }

    public async Task AddPaymentAsync(InvoicePaymentDto dto, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Adding payment for invoice {InvoiceId}, Amount: {Amount}", dto.Id, dto.Amount);
            
            var existing = await _db.Invoices
                .FirstOrDefaultAsync(i => i.Id == dto.Id, ct);

            if (existing is null)
            {
                _logger.LogWarning("Invoice not found for payment: {InvoiceId}", dto.Id);
                return;
            }

            var payment = new InvoicePayment
            {
                InvoiceId = dto.Id,
                Amount = dto.Amount,
                PaymentMode = dto.PaymentMode,
                ChequeNumber = dto.ChequeNumber,
                TransactionReference = dto.TransactionReference,
                PaymentDate = dto.PaymentDate,
                CreatedAt = DateTime.UtcNow
            };

            _db.InvoicePayments.Add(payment);

            var newAdvanceReceived = existing.AdvanceReceived + dto.Amount;
            existing.AdvanceReceived = newAdvanceReceived;

            if (newAdvanceReceived >= existing.NetAmount)
            {
                existing.Status = InvoiceStatus.Paid;
                _logger.LogBusinessEvent("Invoice Fully Paid", new { InvoiceId = dto.Id, TotalAmount = newAdvanceReceived });
                
                if (!string.IsNullOrWhiteSpace(dto.OurOrderReference))
                {
                    var relatedOrder = await _db.Orders
                        .FirstOrDefaultAsync(o => o.OrderNumber == dto.OurOrderReference, ct);

                    if (relatedOrder != null)
                    {
                        await _orderService.UpdateStatusAsync(new OrderUpdateStatusDto
                        {
                            Id = relatedOrder.Id,
                            OrderStatus = (int)OrderStatus.Completed,
                        }, ct);
                    }
                }
            }
            else if (newAdvanceReceived > 0)
            {
                existing.Status = InvoiceStatus.PartiallyPaid;
                _logger.LogBusinessEvent("Invoice Partially Paid", new { 
                    InvoiceId = dto.Id, 
                    PaidAmount = newAdvanceReceived, 
                    RemainingAmount = existing.NetAmount - newAdvanceReceived 
                });
            }

            await _db.SaveChangesAsync(ct);

            _logger.LogBusinessEvent("Payment Recorded", new { 
                InvoiceId = dto.Id, 
                Amount = dto.Amount, 
                PaymentMode = dto.PaymentMode 
            });
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Add Payment", ex, new { InvoiceId = dto.Id, Amount = dto.Amount });
            throw;
        }
    }

    public async Task<InvoiceDto?> GetAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Retrieving invoice {InvoiceId}", id);
            
            var inv = await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                    .ThenInclude(item => item.TaxSettings)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (inv is null)
            {
                _logger.LogWarning("Invoice not found: {InvoiceId}", id);
                return null;
            }

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
                    HsnCode = x.HsnCode,
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
        catch (Exception ex)
        {
            _logger.LogOperationError("Get Invoice", ex, new { InvoiceId = id });
            throw;
        }
    }

    public async Task<PagedResult<InvoiceDto>> SearchAsync(DateTime? from, DateTime? to, int? CustomerId, string? invoiceNumber, int? status, int page, int pageSize, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Searching invoices with filters: From={From}, To={To}, CustomerId={CustomerId}, Status={Status}, Page={Page}", 
                from, to, CustomerId, status, page);
            
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
                .OrderByDescending(x => x.InvoiceDate)
                .ThenBy(x => x.InvoiceNumber)
                .ToListAsync(ct);

            _logger.LogDebug("Invoice search returned {Count} results out of {Total}", items.Count, total);

            return new PagedResult<InvoiceDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Search Invoices", ex, new { From = from, To = to, CustomerId, Status = status });
            throw;
        }
    }

    public async Task<byte[]> GeneratePdfAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Generating PDF for invoice {InvoiceId}", id);
            
            var inv = await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                    .ThenInclude(item => item.TaxSettings)
                .FirstOrDefaultAsync(i => i.Id == id, ct) ?? throw new KeyNotFoundException("Invoice not found");

            var settings = await _db.CompanySettings.FirstOrDefaultAsync(ct) ?? new CompanySettings { CompanyName = "My Company" };

            var doc = new InvoicePdf(settings, inv);
            var pdfBytes = doc.Render();
            
            _logger.LogBusinessEvent("Invoice PDF Generated", new { InvoiceId = id, FileSizeBytes = pdfBytes.Length });

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Generate PDF", ex, new { InvoiceId = id });
            throw;
        }
    }

    public async Task<List<InvoiceDto>> GetOverdueInvoicesAsync(int daysOverdue = 30, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Retrieving overdue invoices (older than {Days} days)", daysOverdue);
            
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

            _logger.LogDebug("Found {Count} overdue invoices", invoices.Count);

            return invoices;
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Get Overdue Invoices", ex, new { DaysOverdue = daysOverdue });
            throw;
        }
    }

    public async Task<int> CreateDraftAsync(InvoiceCreateDto dto, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Creating draft invoice for customer {CustomerId}", dto.CustomerId);
            
            var draftCount = await _db.Invoices.Where(i => i.Status == InvoiceStatus.Draft).CountAsync(ct);
            var invoiceNumber = $"D-{draftCount + 1:0000}";

            var items = dto.Items.Select(i => new InvoiceItem
            {
                Description = i.Description,
                HsnCode = i.HsnCode,
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

            _logger.LogBusinessEvent("Invoice Draft Created", new { 
                InvoiceId = entity.Id, 
                DraftNumber = invoiceNumber, 
                CustomerId = dto.CustomerId 
            });

            return entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Create Draft Invoice", ex, new { CustomerId = dto.CustomerId });
            throw;
        }
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Deleting invoice {InvoiceId}", id);
            
            var existing = await _db.Invoices
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (existing is null)
            {
                _logger.LogWarning("Invoice not found for deletion: {InvoiceId}", id);
                return;
            }

            if (existing.Status != InvoiceStatus.Draft)
            {
                _logger.LogWarning("Attempted to delete non-draft invoice {InvoiceId}, Status: {Status}", id, existing.Status);
                throw new InvalidOperationException("Only draft invoices can be deleted.");
            }

            _db.InvoicePayments.RemoveRange(existing.Payments);
            _db.InvoiceItems.RemoveRange(existing.Items);
            _db.Invoices.Remove(existing);

            await _db.SaveChangesAsync(ct);

            _logger.LogBusinessEvent("Invoice Deleted", new { InvoiceId = id, InvoiceNumber = existing.InvoiceNumber });
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Delete Invoice", ex, new { InvoiceId = id });
            throw;
        }
    }

    private (DateTime StartDate, DateTime EndDate) GetCurrentFinancialYear()
    {
        var currentDate = DateTime.UtcNow;
        var currentYear = currentDate.Year;
        
        DateTime startDate, endDate;
        
        if (currentDate.Month >= 4)
        {
            startDate = new DateTime(currentYear, 4, 1);
            endDate = new DateTime(currentYear + 1, 3, 31, 23, 59, 59);
        }
        else
        {
            startDate = new DateTime(currentYear - 1, 4, 1);
            endDate = new DateTime(currentYear, 3, 31, 23, 59, 59);
        }
        
        return (startDate, endDate);
    }

    public async Task<FinancialYearStatsDto> GetPaidInvoicesStatsForFinancialYearAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Retrieving paid invoices statistics for current financial year");
            
            var (startDate, endDate) = GetCurrentFinancialYear();

            var stats = await _db.Invoices
                .Where(i => i.Status == InvoiceStatus.Paid && 
                           i.InvoiceDate >= startDate && 
                           i.InvoiceDate <= endDate)
                .GroupBy(x => 1)
                .Select(g => new FinancialYearStatsDto
                {
                    TotalCount = g.Count(),
                    TotalAmount = g.Sum(i => i.NetAmount)
                })
                .FirstOrDefaultAsync(ct);

            return stats ?? new FinancialYearStatsDto { TotalCount = 0, TotalAmount = 0 };
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Get Paid Invoices Stats", ex);
            throw;
        }
    }

    public async Task<FinancialYearStatsDto> GetIssuedInvoicesStatsForFinancialYearAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Retrieving issued invoices statistics for current financial year");
            
            var (startDate, endDate) = GetCurrentFinancialYear();

            var stats = await _db.Invoices
                .Where(i => i.Status == InvoiceStatus.Issued && 
                           i.InvoiceDate >= startDate && 
                           i.InvoiceDate <= endDate)
                .GroupBy(x => 1)
                .Select(g => new FinancialYearStatsDto
                {
                    TotalCount = g.Count(),
                    TotalAmount = g.Sum(i => i.NetAmount)
                })
                .FirstOrDefaultAsync(ct);

            return stats ?? new FinancialYearStatsDto { TotalCount = 0, TotalAmount = 0 };
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("Get Issued Invoices Stats", ex);
            throw;
        }
    }
}