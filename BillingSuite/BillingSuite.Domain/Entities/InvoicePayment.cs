namespace BillingSuite.Domain.Entities
{
    public class InvoicePayment
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = default!;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
