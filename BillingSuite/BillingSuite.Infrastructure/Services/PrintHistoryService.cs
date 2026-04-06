using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using BillingSuite.Domain.Entities;
using BillingSuite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingSuite.Infrastructure.Services;

public class PrintHistoryService : IPrintHistoryService
{
    private readonly BillingDbContext _db;
    private readonly ILogger<PrintHistoryService> _logger;

    public PrintHistoryService(BillingDbContext db, ILogger<PrintHistoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RecordPrintAsync(RecordPrintDto dto, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Recording print for invoice {InvoiceId}", dto.InvoiceId);

            var printRecord = new InvoicePrintHistory
            {
                InvoiceId = dto.InvoiceId,
                InvoiceNumber = dto.InvoiceNumber,
                PdfPath = dto.PdfPath,
                PrintedTime = dto.PrintedTime,
                CreatedAt = DateTime.UtcNow
            };

            _db.InvoicePrintHistories.Add(printRecord);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Print recorded for invoice {InvoiceNumber} at {PdfPath}", 
                dto.InvoiceNumber, dto.PdfPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording print for invoice {InvoiceId}", dto.InvoiceId);
            throw;
        }
    }

    public async Task<List<InvoicePrintHistoryDto>> GetPrintHistoryByInvoiceIdAsync(int invoiceId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Retrieving print history for invoice {InvoiceId}", invoiceId);

            var history = await _db.InvoicePrintHistories
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.PrintedTime)
                .Select(p => new InvoicePrintHistoryDto
                {
                    Id = p.Id,
                    InvoiceId = p.InvoiceId,
                    InvoiceNumber = p.InvoiceNumber,
                    PdfPath = p.PdfPath,
                    PrintedTime = p.PrintedTime,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync(ct);

            _logger.LogDebug("Found {Count} print records for invoice {InvoiceId}", history.Count, invoiceId);

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving print history for invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<InvoicePrintHistoryDto?> GetLastPrintAsync(int invoiceId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Retrieving last print record for invoice {InvoiceId}", invoiceId);

            var lastPrint = await _db.InvoicePrintHistories
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.PrintedTime)
                .FirstOrDefaultAsync(ct);

            if (lastPrint is null)
            {
                _logger.LogDebug("No print records found for invoice {InvoiceId}", invoiceId);
                return null;
            }

            return new InvoicePrintHistoryDto
            {
                Id = lastPrint.Id,
                InvoiceId = lastPrint.InvoiceId,
                InvoiceNumber = lastPrint.InvoiceNumber,
                PdfPath = lastPrint.PdfPath,
                PrintedTime = lastPrint.PrintedTime,
                CreatedAt = lastPrint.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving last print for invoice {InvoiceId}", invoiceId);
            throw;
        }
    }
}
