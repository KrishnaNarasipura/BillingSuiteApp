using BillingSuite.Domain.Entities;
using System.Text;

namespace BillingSuite.Infrastructure.Services.Html;

public class OrderHtml
{
    private readonly CompanySettings _settings;
    private readonly Order _order;

    public OrderHtml(CompanySettings settings, Order order)
    {
        _settings = settings;
        _order = order;
    }

    public string Render()
    {
        var html = new StringBuilder();

        html.Append(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Order - ");
        html.Append(_order.OrderNumber);
        html.Append(@"</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            color: #333;
            line-height: 1.6;
            background-color: #f5f5f5;
            padding: 20px;
        }

        .order-container {
            max-width: 900px;
            margin: 0 auto;
            background-color: white;
            padding: 40px;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
        }

        .order-header {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 30px;
            border-bottom: 2px solid #007bff;
            padding-bottom: 20px;
        }

        .company-info {
            flex: 1;
        }

        .company-info h1 {
            color: #007bff;
            font-size: 28px;
            margin-bottom: 10px;
        }

        .company-info p {
            margin: 5px 0;
            font-size: 14px;
            color: #666;
        }

        .order-info {
            text-align: right;
        }

        .order-info h2 {
            color: #0056b3;
            font-size: 24px;
            margin-bottom: 10px;
        }

        .order-info p {
            margin: 5px 0;
            font-size: 13px;
        }

        .order-info .label {
            font-weight: bold;
            color: #333;
        }

        .addresses-section {
            display: flex;
            gap: 30px;
            margin-bottom: 30px;
        }

        .address-box {
            flex: 1;
            border: 1px solid #ddd;
            padding: 15px;
            background-color: #fafafa;
        }

        .address-box h3 {
            background-color: #e9ecef;
            color: #007bff;
            padding: 8px;
            margin: -15px -15px 10px -15px;
            font-size: 14px;
            font-weight: 600;
        }

        .address-box p {
            margin: 5px 0;
            font-size: 13px;
            line-height: 1.5;
        }

        .items-section {
            margin-bottom: 30px;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }

        table thead {
            background-color: #e9ecef;
        }

        table th {
            padding: 12px 8px;
            text-align: left;
            font-weight: 600;
            font-size: 13px;
            border-bottom: 2px solid #dee2e6;
            color: #333;
        }

        table td {
            padding: 10px 8px;
            border-bottom: 1px solid #dee2e6;
            font-size: 13px;
        }

        table td.text-center {
            text-align: center;
        }

        table td.text-right {
            text-align: right;
        }

        table tbody tr:nth-child(even) {
            background-color: #fafafa;
        }

        .summary-section {
            display: flex;
            gap: 30px;
            margin-bottom: 30px;
        }

        .tax-breakdown {
            flex: 1;
        }

        .summary-box {
            flex: 1;
            border: 1px solid #ddd;
            padding: 20px;
            background-color: #fafafa;
        }

        .summary-box h4 {
            margin-bottom: 15px;
            font-size: 14px;
            font-weight: 600;
            color: #333;
        }

        .summary-row {
            display: flex;
            justify-content: space-between;
            margin-bottom: 8px;
            font-size: 13px;
        }

        .summary-row.total-row {
            border-top: 2px solid #007bff;
            border-bottom: 2px solid #007bff;
            padding: 10px 0;
            font-weight: bold;
            font-size: 14px;
            color: #007bff;
            margin-bottom: 10px;
        }

        .amount-words {
            margin-bottom: 30px;
            padding: 15px;
            background-color: #e7f3ff;
            border-left: 4px solid #007bff;
        }

        .amount-words strong {
            display: block;
            margin-bottom: 5px;
        }

        .terms-conditions {
            margin-bottom: 30px;
            padding: 15px;
            border: 1px solid #ddd;
            background-color: #fafafa;
        }

        .terms-conditions h4 {
            margin-bottom: 10px;
            font-size: 13px;
            font-weight: 600;
        }

        .terms-conditions p {
            margin-bottom: 5px;
            font-size: 12px;
            line-height: 1.4;
        }

        .signature-section {
            display: flex;
            justify-content: flex-end;
            margin-top: 50px;
        }

        .signature-box {
            width: 250px;
            border: 1px solid #333;
            padding: 20px;
            text-align: center;
        }

        .signature-box p {
            margin: 10px 0;
            font-size: 12px;
        }

        .signature-line {
            border-top: 1px solid #333;
            margin-top: 40px;
            padding-top: 10px;
        }

        .footer {
            text-align: center;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            font-size: 12px;
            color: #666;
        }

        .footer p {
            margin: 3px 0;
        }

        .print-button {
            margin-bottom: 20px;
            text-align: center;
        }

        .print-button button {
            background-color: #0056b3;
            color: white;
            border: none;
            padding: 10px 30px;
            font-size: 14px;
            border-radius: 4px;
            cursor: pointer;
            margin-right: 10px;
        }

        .print-button button:hover {
            background-color: #004085;
        }

        .print-button button.secondary {
            background-color: #6c757d;
        }

        .print-button button.secondary:hover {
            background-color: #5a6268;
        }

        .status-badge {
            display: inline-block;
            padding: 4px 8px;
            border-radius: 3px;
            font-size: 12px;
            font-weight: 600;
        }

        .status-draft {
            background-color: #e2e3e5;
            color: #383d41;
        }

        .status-confirmed {
            background-color: #cce5ff;
            color: #004085;
        }

        .status-invoiced {
            background-color: #d1ecf1;
            color: #0c5460;
        }

        .status-completed {
            background-color: #d4edda;
            color: #155724;
        }

        @media print {
            body {
                background-color: white;
                padding: 0;
            }

            .order-container {
                box-shadow: none;
                max-width: 100%;
                margin: 0;
            }

            .print-button {
                display: none;
            }

            .summary-section {
                display: block;
            }

            .tax-breakdown {
                margin-bottom: 20px;
            }

            @page {
                margin: 0.5in;
            }
        }

        .text-muted {
            color: #6c757d;
        }
    </style>
</head>
<body>");

        // Print button
        html.Append(@"
    <div class='print-button'>
        <button onclick='window.print()'>
            <span>??? Print Order</span>
        </button>
        <button class='secondary' onclick='window.history.back()'>
            <span>? Back</span>
        </button>
    </div>");

        // Order container
        html.Append(@"
    <div class='order-container'>");

        // Header
        html.Append(@"
        <div class='order-header'>
            <div class='company-info'>
                <h1>");
        html.Append(_settings.CompanyName);
        html.Append(@"</h1>");

        if (!string.IsNullOrWhiteSpace(_settings.Address))
        {
            html.Append("<p>");
            html.Append(HtmlEncode(_settings.Address));
            html.Append("</p>");
        }

        if (!string.IsNullOrWhiteSpace(_settings.Phone))
        {
            html.Append("<p><strong>Phone:</strong> ");
            html.Append(HtmlEncode(_settings.Phone));
            html.Append("</p>");
        }

        if (!string.IsNullOrWhiteSpace(_settings.Gstin))
        {
            html.Append("<p><strong>GSTIN:</strong> ");
            html.Append(HtmlEncode(_settings.Gstin));
            html.Append("</p>");
        }

        html.Append(@"
            </div>
            <div class='order-info'>
                <h2>ORDER");
        
        // Add status badge
        var statusClass = _order.Status.ToString() switch
        {
            "Draft" => "status-draft",
            "Confirmed" => "status-confirmed",
            "InvoiceIssued" => "status-invoiced",
            "Completed" => "status-completed",
            _ => ""
        };

        html.Append(@"
                </h2>
                <p><span class='label'>Order #:</span> ");
        html.Append(HtmlEncode(_order.OrderNumber));
        html.Append(@"</p>
                <p><span class='label'>Date:</span> ");
        html.Append(_order.OrderDate.ToString("dd-MMM-yyyy"));
        html.Append(@"</p>
                <p><span class='label'>Status:</span> <span class='status-badge ");
        html.Append(statusClass);
        html.Append(@"'>");
        html.Append(_order.Status.ToString());
        html.Append(@"</span></p>");

        if (!string.IsNullOrWhiteSpace(_order.YourOrderReference))
        {
            html.Append("<p><span class='label'>Your Order Ref:</span> ");
            html.Append(HtmlEncode(_order.YourOrderReference));
            html.Append("</p>");
        }

        html.Append(@"
            </div>
        </div>");

        // Addresses
        html.Append(@"
        <div class='addresses-section'>
            <div class='address-box'>
                <h3>BILL TO</h3>
                <p><strong>");
        html.Append(HtmlEncode(_order.Customer.Name));
        html.Append(@"</strong></p>");

        if (!string.IsNullOrWhiteSpace(_order.Customer.BillingAddress))
        {
            var lines = _order.Customer.BillingAddress.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                html.Append("<p>");
                html.Append(HtmlEncode(line.Trim()));
                html.Append("</p>");
            }
        }

        if (!string.IsNullOrWhiteSpace(_order.Customer.Email))
        {
            html.Append("<p><strong>Email:</strong> ");
            html.Append(HtmlEncode(_order.Customer.Email));
            html.Append("</p>");
        }

        if (!string.IsNullOrWhiteSpace(_order.Customer.Phone))
        {
            html.Append("<p><strong>Phone:</strong> ");
            html.Append(HtmlEncode(_order.Customer.Phone));
            html.Append("</p>");
        }

        if (!string.IsNullOrWhiteSpace(_order.Customer.Gstin))
        {
            html.Append("<p><strong>GSTIN:</strong> ");
            html.Append(HtmlEncode(_order.Customer.Gstin));
            html.Append("</p>");
        }

        html.Append(@"
            </div>
            <div class='address-box'>
                <h3>SHIP TO</h3>");

        if (!string.IsNullOrWhiteSpace(_order.Customer.ShippingAddress) &&
            _order.Customer.ShippingAddress != _order.Customer.BillingAddress)
        {
            html.Append("<p><strong>");
            html.Append(HtmlEncode(_order.Customer.Name));
            html.Append("</strong></p>");

            var lines = _order.Customer.ShippingAddress.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                html.Append("<p>");
                html.Append(HtmlEncode(line.Trim()));
                html.Append("</p>");
            }
        }
        else
        {
            html.Append("<p class='text-muted'>Same as Billing Address</p>");
        }

        html.Append(@"
            </div>
        </div>");

        // Items Table
        html.Append(@"
        <div class='items-section'>
            <table>
                <thead>
                    <tr>
                        <th style='width: 5%;'>Sl.No</th>
                        <th style='width: 35%;'>Description</th>
                        <th style='width: 10%;' class='text-center'>HSN Code</th>
                        <th style='width: 8%;' class='text-center'>Qty</th>
                        <th style='width: 12%;' class='text-right'>Unit Price</th>
                        <th style='width: 12%;' class='text-center'>Tax Type</th>
                        <th style='width: 10%;' class='text-right'>Line Total</th>
                        <th style='width: 10%;' class='text-right'>Tax Amount</th>
                        <th style='width: 10%;' class='text-right'>Item Total</th>
                    </tr>
                </thead>
                <tbody>");

        var serialNo = 1;
        foreach (var item in _order.Items)
        {
            var itemTotal = item.LineTotal + item.TaxAmount;
            var taxType = item.TaxSettings?.TaxType ?? "";
            var taxPercent = item.TaxSettings?.TaxPercent ?? 0;
            var taxDisplay = !string.IsNullOrEmpty(taxType) ? $"{taxType} ({taxPercent}%)" : "No Tax";

            html.Append("<tr>");
            html.Append("<td class='text-center'>");
            html.Append(serialNo);
            html.Append("</td>");

            html.Append("<td>");
            html.Append(HtmlEncode(item.Description));
            html.Append("</td>");

            html.Append("<td class='text-center'>");
            html.Append(HtmlEncode(item.HsnCode ?? "N/A"));
            html.Append("</td>");

            html.Append("<td class='text-center'>");
            html.Append(item.Quantity.ToString("0.##"));
            html.Append("</td>");

            html.Append("<td class='text-right'>");
            html.Append(item.UnitPrice.ToString("N2"));
            html.Append("</td>");

            html.Append("<td class='text-center'>");
            html.Append(HtmlEncode(taxDisplay));
            html.Append("</td>");

            html.Append("<td class='text-right'>");
            html.Append(item.LineTotal.ToString("N2"));
            html.Append("</td>");

            html.Append("<td class='text-right'>");
            html.Append(item.TaxAmount.ToString("N2"));
            html.Append("</td>");

            html.Append("<td class='text-right'><strong>");
            html.Append(itemTotal.ToString("N2"));
            html.Append("</strong></td>");

            html.Append("</tr>");

            serialNo++;
        }

        html.Append(@"
                </tbody>
            </table>
        </div>");

        // Summary Section
        html.Append(@"
        <div class='summary-section'>");

        // Tax Breakdown
        var taxBreakdown = _order.Items
            .Where(item => item.TaxSettings != null && item.TaxAmount > 0)
            .GroupBy(item => new { item.TaxSettings!.TaxType, item.TaxSettings.TaxPercent })
            .Select(g => new { g.Key.TaxType, g.Key.TaxPercent, Amount = g.Sum(x => x.TaxAmount) })
            .ToList();

        if (taxBreakdown.Any())
        {
            html.Append(@"
            <div class='tax-breakdown'>
                <div class='summary-box'>
                    <h4>Tax Breakdown</h4>");

            foreach (var tax in taxBreakdown)
            {
                html.Append(@"
                    <div class='summary-row'>
                        <span>");
                html.Append(tax.TaxType);
                html.Append(" (");
                html.Append(tax.TaxPercent);
                html.Append("%)</span>");
                html.Append("<span>");
                html.Append(tax.Amount.ToString("N2"));
                html.Append("</span>");
                html.Append(@"
                    </div>");
            }

            html.Append(@"
                </div>
            </div>");
        }

        // Summary Totals
        html.Append(@"
            <div>
                <div class='summary-box'>
                    <h4>Order Summary</h4>
                    <div class='summary-row'>
                        <span>Subtotal:</span>
                        <span>");
        html.Append(_order.Subtotal.ToString("N2"));
        html.Append(@"</span>
                    </div>
                    <div class='summary-row'>
                        <span>Total Tax:</span>
                        <span>");
        html.Append(_order.TaxAmount.ToString("N2"));
        html.Append(@"</span>
                    </div>");

        if (_order.DiscountAmount > 0)
        {
            html.Append(@"
                    <div class='summary-row' style='color: #dc3545;'>
                        <span>Discount:</span>
                        <span>-");
            html.Append(_order.DiscountAmount.ToString("N2"));
            html.Append(@"</span>
                    </div>");
        }

        html.Append(@"
                    <div class='summary-row total-row'>
                        <span>Net Amount:</span>
                        <span>");
        html.Append(_order.NetAmount.ToString("N2"));
        html.Append(@"</span>
                    </div>
                </div>
            </div>
        </div>");

        // Amount in Words
        var amountInWords = BillingSuite.Domain.Utility.ConvertNumberToWords(_order.NetAmount);
        html.Append(@"
        <div class='amount-words'>
            <strong>Amount in Words:</strong>
            <span>");
        html.Append(HtmlEncode(amountInWords));
        html.Append(@"</span>
        </div>");

        // Terms and Conditions
        if (!string.IsNullOrWhiteSpace(_settings.TermsAndConditions))
        {
            html.Append(@"
        <div class='terms-conditions'>
            <h4>Terms & Conditions</h4>");

            var termsLines = _settings.TermsAndConditions
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in termsLines.Take(3))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    html.Append("<p>");
                    html.Append(HtmlEncode(line.Trim()));
                    html.Append("</p>");
                }
            }

            html.Append(@"
        </div>");
        }
        else
        {
            html.Append(@"
        <div class='terms-conditions'>
            <h4>Terms & Conditions</h4>
            <p>1. Delivery as per agreed schedule.</p>
            <p>2. Payment terms as per agreement.</p>
        </div>");
        }

        // Signature Section
        html.Append(@"
        <div class='signature-section'>
            <div class='signature-box'>
                <p><strong>For ");
        html.Append(HtmlEncode(_settings.CompanyName));
        html.Append(@"</strong></p>
                <p>Authorized Signatory</p>
                <div class='signature-line'>
                    <p>Signature</p>
                    <p style='font-size: 11px; margin-top: 20px;'>Name & Date</p>
                </div>
            </div>
        </div>");

        // Footer
        html.Append(@"
        <div class='footer'>
            <p>Thank you for your business!</p>
            <p>Generated by BillingSuite</p>
            <p>Generated on: ");
        html.Append(DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
        html.Append(@"</p>
        </div>
    </div>
</body>
</html>");

        return html.ToString();
    }

    private string HtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return System.Web.HttpUtility.HtmlEncode(text);
    }
}
