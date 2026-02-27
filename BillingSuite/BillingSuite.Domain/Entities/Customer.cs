namespace BillingSuite.Domain.Entities;

public class Customer
{

    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Gstin { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

}
