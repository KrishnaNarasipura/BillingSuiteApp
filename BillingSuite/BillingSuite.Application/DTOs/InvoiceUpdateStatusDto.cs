using BillingSuite.Application.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.DTOs
{
    public class InvoiceUpdateStatusDto
    {
        public int Id { get; set; }
        public InvoiceStatus InvoiceStatus { get; set; }
    }
}
