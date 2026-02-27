using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Domain.Entities
{
    public class CompanySettings
    {

        public int Id { get; set; }
        public string CompanyName { get; set; } = default!;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Gstin { get; set; }
        public byte[]? LogoBytes { get; set; }

        public string? TermsAndConditions { get; set; }
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

    }
}
