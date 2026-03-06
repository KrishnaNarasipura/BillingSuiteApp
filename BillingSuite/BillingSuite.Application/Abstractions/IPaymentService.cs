using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;

namespace BillingSuite.Application.Abstractions;

public interface IPaymentService
{
    Task<PagedResult<PaymentViewDto>> SearchAsync(DateTime? from, DateTime? to, int? customerId, string? invoiceNumber, string? orderReference, int page, int pageSize, CancellationToken ct = default);
    Task<List<PaymentViewDto>> GetPaymentsByInvoiceIdAsync(int invoiceId, CancellationToken ct = default);
    Task<PaymentViewDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<decimal> GetTotalPaymentsByInvoiceIdAsync(int invoiceId, CancellationToken ct = default);
    Task<List<PaymentViewDto>> GetRecentPaymentsAsync(int count = 10, CancellationToken ct = default);
    Task UpdateAsync(PaymentEditDto dto, CancellationToken ct = default);
}