using BillingSuite.Domain.Enums;

namespace BillingSuite.Application.DTOs;

public class PaymentViewDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMode PaymentMode { get; set; }
    public string? ChequeNumber { get; set; }
    public string? TransactionReference { get; set; }
    public string? OurOrderReference { get; set; }
    public DateTime CreatedAt { get; set; }
}