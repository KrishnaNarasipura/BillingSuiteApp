using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.DTOs
{
    public class OrderItemDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = default!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public int? TaxSettingsId { get; set; }
        public decimal TaxAmount { get; set; }
    }
}
