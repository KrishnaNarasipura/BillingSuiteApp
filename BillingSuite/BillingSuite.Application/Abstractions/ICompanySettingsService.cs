using BillingSuite.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.Abstractions
{

    public interface ICompanySettingsService
    {
        Task<CompanySettings> GetAsync(CancellationToken ct = default);
        Task UpdateAsync(CompanySettings settings, CancellationToken ct = default);
    }

}
