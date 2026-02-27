using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Domain.Entities
{
    public class TaxSettings
    {
        public int Id { get; set; }
        public decimal TaxPercent { get; set; }
        public string TaxType { get; set; } = default!;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    }
}
