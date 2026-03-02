using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = default!;
        public DateTime OrderDate { get; set; }
        public CustomerDto Customer { get; set; } = default!;
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal AdvanceReceived { get; set; }
        public string? YourOrderReference { get; set; }
        public int Status { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
