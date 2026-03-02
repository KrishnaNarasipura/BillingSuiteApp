using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.DTOs
{
    public class OrderEditDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = default!;
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal AdvanceReceived { get; set; }
        public string? YourOrderReference { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
