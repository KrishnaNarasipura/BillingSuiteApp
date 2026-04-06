using BillingSuite.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillingSuite.Infrastructure.Services;

/// <summary>
/// Service for saving PDF files to disk
/// </summary>
public class PdfSaveService
{
    private readonly PdfSettings _pdfSettings;
    private readonly ILogger<PdfSaveService> _logger;

    public PdfSaveService(IOptions<PdfSettings> pdfSettings, ILogger<PdfSaveService> logger)
    {
        _pdfSettings = pdfSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Saves a PDF file and returns the full path
    /// </summary>
    public async Task<string> SavePdfAsync(byte[] pdfContent, string invoiceNumber, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Saving PDF for invoice {InvoiceNumber}", invoiceNumber);

            // Create directory if it doesn't exist
            var basePath = _pdfSettings.SavePath;
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), basePath);

            if (_pdfSettings.CreateDirectoryIfNotExists && !Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                _logger.LogInformation("Created PDF save directory: {Path}", fullPath);
            }

            // Generate filename
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = _pdfSettings.FileNameFormat
                .Replace("{invoiceNumber}", invoiceNumber)
                .Replace("{timestamp}", timestamp);

            var filePath = Path.Combine(fullPath, fileName);

            // Save file
            await File.WriteAllBytesAsync(filePath, pdfContent, ct);

            _logger.LogInformation("PDF saved successfully for invoice {InvoiceNumber} at {FilePath}", invoiceNumber, filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving PDF for invoice {InvoiceNumber}", invoiceNumber);
            throw;
        }
    }

    /// <summary>
    /// Checks if a PDF file exists at the specified path
    /// </summary>
    public bool PdfExists(string pdfPath)
    {
        return File.Exists(pdfPath);
    }

    /// <summary>
    /// Gets the relative path from the PDF save directory
    /// </summary>
    public string GetRelativePath(string fullPath)
    {
        var basePath = _pdfSettings.SavePath;
        var baseDir = Path.Combine(Directory.GetCurrentDirectory(), basePath);
        
        if (fullPath.StartsWith(baseDir))
        {
            return fullPath.Substring(baseDir.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        return fullPath;
    }
}
