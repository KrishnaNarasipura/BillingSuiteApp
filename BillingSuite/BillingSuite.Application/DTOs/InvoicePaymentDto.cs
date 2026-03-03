namespace BillingSuite.Application.DTOs
{
    public class InvoicePaymentDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
