namespace BillingSuite.Domain.Entities;

/// <summary>
/// Tracks invoice print history including PDF location and print timestamp
/// </summary>
public class InvoicePrintHistory
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PdfPath { get; set; } = string.Empty;
    public DateTime PrintedTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
