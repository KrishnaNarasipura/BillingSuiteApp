using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;
using BillingSuite.Domain.Entities;
using BillingSuite.Domain.Enums;
using BillingSuite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingSuite.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly BillingDbContext _db;
    public OrderService(BillingDbContext db) => _db = db;

    public async Task<int> CreateAsync(OrderCreateDto dto, CancellationToken ct = default)
    {
        // Generate order number YYYYMM-#### (for demo)
        var datePrefix = dto.OrderDate.ToString("yyyyMM");
        var countThisMonth = await _db.Orders.CountAsync(o => o.OrderDate.Year == dto.OrderDate.Year && o.OrderDate.Month == dto.OrderDate.Month, ct);
        var orderNumber = $"VC-{datePrefix}-{countThisMonth + 1:0000}";

        var items = dto.Items.Select(i => new OrderItem
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

        var entity = new Order
        {
            CustomerId = dto.CustomerId,
            OrderDate = dto.OrderDate,
            OrderNumber = orderNumber,
            YourOrderReference = dto.YourOrderReference,
            Subtotal = subtotal,
            Status = OrderStatus.Confirmed,
            TaxAmount = totalTax,
            DiscountAmount = dto.DiscountAmount,
            AdvanceReceived = dto.AdvanceReceived,
            NetAmount = net,
            Items = items
        };

        _db.Orders.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(OrderEditDto dto, CancellationToken ct = default)
    {
        var existing = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == dto.Id, ct);

        if (existing is null) return;

        // Remove existing items
        _db.OrderItems.RemoveRange(existing.Items);

        // Add updated items
        var items = dto.Items.Select(i => new OrderItem
        {
            OrderId = dto.Id,
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
        existing.OrderNumber = dto.OrderNumber;
        existing.OrderDate = dto.OrderDate;
        existing.Subtotal = subtotal;
        existing.TaxAmount = totalTax;
        existing.DiscountAmount = dto.DiscountAmount;
        existing.AdvanceReceived = dto.AdvanceReceived;
        existing.YourOrderReference = dto.YourOrderReference;
        existing.NetAmount = net;

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(OrderUpdateStatusDto dto, CancellationToken ct = default)
    {
        var existing = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == dto.Id, ct);

        if (existing is null) return;
        existing.Status = (OrderStatus)dto.OrderStatus;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<OrderDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(item => item.TaxSettings)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null) return null;

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            Customer = new CustomerDto
            {
                Id = order.Customer.Id,
                Name = order.Customer.Name,
                BillingAddress = order.Customer.BillingAddress,
                ShippingAddress = order.Customer.ShippingAddress,
                Email = order.Customer.Email,
                Phone = order.Customer.Phone,
                Gstin = order.Customer.Gstin
            },
            Subtotal = order.Subtotal,
            TaxAmount = order.TaxAmount,
            DiscountAmount = order.DiscountAmount,
            AdvanceReceived = order.AdvanceReceived,
            YourOrderReference = order.YourOrderReference,
            NetAmount = order.NetAmount,
            Status = (int)order.Status,
            Items = order.Items.Select(x => new OrderItemDto
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

    public async Task<PagedResult<OrderDto>> SearchAsync(DateTime? from, DateTime? to, int? CustomerId, string? orderNumber, int? status, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Orders
            .Include(o => o.Customer)
            .AsQueryable();

        if (from.HasValue) q = q.Where(o => o.OrderDate >= from.Value);
        if (to.HasValue) q = q.Where(o => o.OrderDate <= to.Value);
        if (CustomerId.HasValue) q = q.Where(o => o.CustomerId == CustomerId);
        if (!string.IsNullOrWhiteSpace(orderNumber)) q = q.Where(o => o.OrderNumber == orderNumber);
        if (status.HasValue) q = q.Where(o => (int)o.Status == status.Value);

        var total = await q.CountAsync(ct);

        var items = await q.OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(order => new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Customer = new CustomerDto { Id = order.Customer.Id, Name = order.Customer.Name },
                Subtotal = order.Subtotal,
                TaxAmount = order.TaxAmount,
                DiscountAmount = order.DiscountAmount,
                AdvanceReceived = order.AdvanceReceived,
                YourOrderReference = order.YourOrderReference,
                NetAmount = order.NetAmount,
                Status = (int)order.Status
            })
            .ToListAsync(ct);

        return new PagedResult<OrderDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<List<OrderDto>> GetPendingOrdersAsync(CancellationToken ct = default)
    {
        var orders = await _db.Orders
            .Include(o => o.Customer)
            .Where(o => o.Status == OrderStatus.Draft || o.Status == OrderStatus.Confirmed)
            .OrderBy(o => o.OrderDate)
            .Select(order => new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Customer = new CustomerDto { Id = order.Customer.Id, Name = order.Customer.Name },
                Subtotal = order.Subtotal,
                TaxAmount = order.TaxAmount,
                DiscountAmount = order.DiscountAmount,
                AdvanceReceived = order.AdvanceReceived,
                NetAmount = order.NetAmount,
                Status = (int)order.Status
            })
            .ToListAsync(ct);

        return orders;
    }

    public async Task<int> CreateDraftAsync(OrderCreateDto dto, CancellationToken ct = default)
    {
        // Draft order number generator D-#### (sequential)
        var draftCount = await _db.Orders.Where(o => o.Status == OrderStatus.Draft).CountAsync(ct);
        var orderNumber = $"DO-{draftCount + 1:0000}";

        var items = dto.Items.Select(i => new OrderItem
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

        var entity = new Order
        {
            CustomerId = dto.CustomerId,
            OrderDate = dto.OrderDate,
            OrderNumber = orderNumber,
            Subtotal = subtotal,
            TaxAmount = totalTax,
            DiscountAmount = dto.DiscountAmount,
            AdvanceReceived = dto.AdvanceReceived,
            NetAmount = net,
            Status = OrderStatus.Draft,
            Items = items
        };

        _db.Orders.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
