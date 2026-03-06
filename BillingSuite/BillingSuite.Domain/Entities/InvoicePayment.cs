using BillingSuite.Domain.Enums;

namespace BillingSuite.Domain.Entities
{
    public class InvoicePayment
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = default!;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;
        public string? ChequeNumber { get; set; }
        public string? TransactionReference { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
