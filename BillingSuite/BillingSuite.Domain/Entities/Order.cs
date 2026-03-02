using BillingSuite.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = default!;
        public string? YourOrderReference { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = default!;

        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal AdvanceReceived { get; set; } = 0;

        public OrderStatus Status { get; set; } = OrderStatus.Draft;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
