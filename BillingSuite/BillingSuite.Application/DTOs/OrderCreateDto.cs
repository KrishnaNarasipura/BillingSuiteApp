using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.DTOs
{
    public class OrderCreateDto
    {
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal DiscountAmount { get; set; }
        public decimal AdvanceReceived { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
