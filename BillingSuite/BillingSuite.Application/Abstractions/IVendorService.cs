using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;

namespace BillingSuite.Application.Abstractions;


public interface IVendorService
{
    Task<PagedResult<VendorDto>> GetVendorsAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<VendorDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(VendorDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, VendorDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

