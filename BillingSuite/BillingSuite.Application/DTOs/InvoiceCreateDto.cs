using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.DTOs
{
    public class InvoiceCreateDto
    {

        public int CustomerId { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public decimal DiscountAmount { get; set; }
        public decimal AdvanceReceived { get; set; }
        public decimal TaxPercent { get; set; } // simple tax
        public string OurOrderReference { get; set; }
        public string YourOrderReference { get; set; }
        public List<InvoiceItemDto> Items { get; set; } = new();

    }
}
