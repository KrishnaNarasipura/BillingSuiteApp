namespace BillingSuite.Application.Configuration;

/// <summary>
/// Configuration settings for PDF generation and storage
/// </summary>
public class PdfSettings
{
    public string SavePath { get; set; } = "GeneratedPdfs";
    public bool CreateDirectoryIfNotExists { get; set; } = true;
    public string FileNameFormat { get; set; } = "invoice-{invoiceNumber}-{timestamp}.pdf";
}
