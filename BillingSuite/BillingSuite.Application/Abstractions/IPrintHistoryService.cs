using BillingSuite.Application.DTOs;

namespace BillingSuite.Application.Abstractions;

/// <summary>
/// Service for managing invoice print history
/// </summary>
public interface IPrintHistoryService
{
    /// <summary>
    /// Records a print event for an invoice
    /// </summary>
    Task RecordPrintAsync(RecordPrintDto dto, CancellationToken ct = default);

    /// <summary>
    /// Gets print history for an invoice
    /// </summary>
    Task<List<InvoicePrintHistoryDto>> GetPrintHistoryByInvoiceIdAsync(int invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Gets the last print record for an invoice
    /// </summary>
    Task<InvoicePrintHistoryDto?> GetLastPrintAsync(int invoiceId, CancellationToken ct = default);
}
