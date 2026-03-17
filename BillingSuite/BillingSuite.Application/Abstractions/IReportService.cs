using BillingSuite.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.Abstractions
{

    public interface IReportService
    {
        Task<List<SalesSummaryDto>> GetSalesSummaryAsync(DateTime from, DateTime to, int? CustomerId, CancellationToken ct = default);
        Task<string> SalesSummaryHtmlAsync(DateTime from, DateTime to, int? CustomerId, CancellationToken ct = default);
        Task<List<TaxSummaryDto>> GetTaxSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
        Task<string> TaxSummaryHtmlAsync(DateTime from, DateTime to, CancellationToken ct = default);
    }

    public class SalesSummaryDto
    {
        public DateTime Period { get; set; }
        public int Count { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Net { get; set; }
    }

}
