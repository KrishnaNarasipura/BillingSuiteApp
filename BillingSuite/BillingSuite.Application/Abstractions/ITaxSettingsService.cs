using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;
using BillingSuite.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.Abstractions
{

    public interface ITaxSettingsService
    {
        Task<PagedResult<TaxSettingsDto>> GetAsync(CancellationToken ct = default);
        Task UpdateAsync(TaxSettings settings, CancellationToken ct = default);
        Task<int> CreateAsync(TaxSettings settings, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }

}
