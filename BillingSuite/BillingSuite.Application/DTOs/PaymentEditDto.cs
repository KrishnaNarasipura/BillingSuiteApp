using BillingSuite.Domain.Enums;

namespace BillingSuite.Application.DTOs
{
    public class PaymentEditDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;
        public string? ChequeNumber { get; set; }
        public string? TransactionReference { get; set; }
    }
}