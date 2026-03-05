using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Domain.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = default!;

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
