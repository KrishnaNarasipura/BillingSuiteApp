using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.DTOs
{
    public class TaxSummaryDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = default!;
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string HsnCode { get; set; } = default!;
        public string TaxType { get; set; } = default!;
        public decimal TaxPercent { get; set; }
        public decimal LineTotal { get; set; }
        public decimal TaxAmount { get; set; }
    }
}
