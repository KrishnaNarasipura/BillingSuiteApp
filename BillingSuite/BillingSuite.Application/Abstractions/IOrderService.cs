using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;

namespace BillingSuite.Application.Abstractions
{
    public interface IOrderService
    {
        Task<int> CreateAsync(OrderCreateDto dto, CancellationToken ct = default);
        Task<int> CreateDraftAsync(OrderCreateDto dto, CancellationToken ct = default);
        Task UpdateAsync(OrderEditDto dto, CancellationToken ct = default);
        Task UpdateStatusAsync(OrderUpdateStatusDto dto, CancellationToken ct = default);
        Task<OrderDto?> GetAsync(int id, CancellationToken ct = default);
        Task<PagedResult<OrderDto>> SearchAsync(DateTime? from, DateTime? to, int? CustomerId, string? orderNumber, int? status, int page, int pageSize, CancellationToken ct = default);
        Task<List<OrderDto>> GetPendingOrdersAsync(CancellationToken ct = default);
    }
}
