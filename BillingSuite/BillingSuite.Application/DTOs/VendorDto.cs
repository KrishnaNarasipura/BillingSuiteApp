using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Application.DTOs
{
    public class VendorDto
    {

        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? BillingAddress { get; set; }
        public string? ShippingAddress { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Gstin { get; set; }

    }
}
