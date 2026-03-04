using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Domain.Enums
{
    public enum OrderStatus
    {
        Draft = 0,
        Confirmed = 1,
        InvoiceIssued = 2,
        Cancelled = 3,
        Completed = 4
    }
}
