using BillingSuite.Application.Abstractions;
using BillingSuite.Application.Results;
using BillingSuite.Application.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BillingSuite.Web.Pages.Invoices;

public class IndexModel : PageModel
{
    private readonly IInvoiceService _invoiceService;

    public IndexModel(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    public PagedResult<InvoiceDto> Invoices { get; set; } = new();

    public async Task OnGetAsync(DateTime? from, DateTime? to, int? vendorId, int page = 1, int pageSize = 20)
    {
        Invoices = await _invoiceService.SearchAsync(from, to, vendorId, page, pageSize);
    }
}