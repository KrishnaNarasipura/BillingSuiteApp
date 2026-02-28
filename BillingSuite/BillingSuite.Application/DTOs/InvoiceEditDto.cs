using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.DTOs
{
    public class InvoiceEditDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = default!;
        public int CustomerId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal AdvanceReceived { get; set; }
        public List<InvoiceItemDto> Items { get; set; } = new();
    }
}