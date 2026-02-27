using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.Abstractions
{

    public interface IReportService
    {
        Task<byte[]> SalesSummaryPdfAsync(DateTime from, DateTime to, int? vendorId, CancellationToken ct = default);
    }

}
