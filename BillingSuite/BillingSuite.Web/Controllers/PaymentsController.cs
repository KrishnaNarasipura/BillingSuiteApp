using BillingSuite.Application.Abstractions;
using BillingSuite.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BillingSuite.Web.Controllers;

public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly ICustomerService _customerService;

    public PaymentsController(IPaymentService paymentService, ICustomerService customerService)
    {
        _paymentService = paymentService;
        _customerService = customerService;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? customerId, string? invoiceNumber, string? orderReference, int page = 1, int pageSize = 20)
    {
        // Get customers for the dropdown filter
        ViewBag.Customers = (await _customerService.GetCustomersAsync(null, 1, 500)).Items;

        // Use the payment service to get the paginated results
        var result = await _paymentService.SearchAsync(from, to, customerId, invoiceNumber, orderReference, page, pageSize);

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetPayment(int id)
    {
        var payment = await _paymentService.GetByIdAsync(id);
        if (payment == null)
        {
            return Json(new { success = false, message = "Payment not found" });
        }

        return Json(new
        {
            success = true,
            payment = new
            {
                id = payment.Id,
                amount = payment.Amount,
                paymentDate = payment.PaymentDate.ToString("yyyy-MM-dd"),
                paymentMode = (int)payment.PaymentMode,
                chequeNumber = payment.ChequeNumber,
                transactionReference = payment.TransactionReference,
                invoiceNumber = payment.InvoiceNumber,
                customerName = payment.CustomerName
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePayment([FromBody] PaymentEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data provided" });
        }

        try
        {
            await _paymentService.UpdateAsync(dto);
            return Json(new { success = true, message = "Payment updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error updating payment: " + ex.Message });
        }
    }
}