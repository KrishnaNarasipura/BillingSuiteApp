using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.DTOs
{
    public class InvoiceDto
    {

        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = default!;
        public DateTime InvoiceDate { get; set; }
        public CustomerDto Customer { get; set; } = default!;
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal AdvanceReceived { get; set; }
        public int Status { get; set; }
        public List<InvoiceItemDto> Items { get; set; } = new();

    }
}
