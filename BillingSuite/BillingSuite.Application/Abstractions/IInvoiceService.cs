using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.Abstractions
{

    public interface IInvoiceService
    {
        Task<int> CreateAsync(InvoiceCreateDto dto, CancellationToken ct = default);
        Task<InvoiceDto?> GetAsync(int id, CancellationToken ct = default);
        Task<PagedResult<InvoiceDto>> SearchAsync(DateTime? from, DateTime? to, int? vendorId, int page, int pageSize, CancellationToken ct = default);
        Task<byte[]> GeneratePdfAsync(int id, CancellationToken ct = default); // printing/reprint

        Task UpdateAsync(InvoiceEditDto dto, CancellationToken ct = default);
    }

}
