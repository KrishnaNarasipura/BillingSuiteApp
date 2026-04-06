namespace BillingSuite.Application.DTOs;

public class InvoicePrintHistoryDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PdfPath { get; set; } = string.Empty;
    public DateTime PrintedTime { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RecordPrintDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PdfPath { get; set; } = string.Empty;
    public DateTime PrintedTime { get; set; }
}
