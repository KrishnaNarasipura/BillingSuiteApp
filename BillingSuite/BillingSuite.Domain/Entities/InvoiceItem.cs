using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Domain.Entities
{
    public class InvoiceItem
    {

        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string HsnCode { get; set; } = default!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public int? TaxSettingsId { get; set; }
        public TaxSettings? TaxSettings { get; set; }
        public decimal TaxAmount { get; set; }

    }
}
