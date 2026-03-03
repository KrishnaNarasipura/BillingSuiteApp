namespace BillingSuite.Application.DTOs
{
    public class PaymentHistoryDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
