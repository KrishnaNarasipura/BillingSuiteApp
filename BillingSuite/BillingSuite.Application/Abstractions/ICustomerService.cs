using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;

namespace BillingSuite.Application.Abstractions;


public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CustomerDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, CustomerDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

