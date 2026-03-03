using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.Enums
{

    public enum InvoiceStatus
    {
        Draft = 0,
        Issued = 1,
        Cancelled = 2,
        Paid = 3,
        PartiallyPaid = 4
    }


}
