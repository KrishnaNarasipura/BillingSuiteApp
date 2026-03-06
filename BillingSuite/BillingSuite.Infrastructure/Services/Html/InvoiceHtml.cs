using BillingSuite.Domain.Entities;
using System.Text;

namespace BillingSuite.Infrastructure.Services.Html;

public class InvoiceHtml
{
    private readonly CompanySettings _settings;
    private readonly Invoice _invoice;
    private readonly string _templatePath;

    public InvoiceHtml(CompanySettings settings, Invoice invoice)
    {
        _settings = settings;
        _invoice = invoice;
        // Get the template path relative to the application
        _templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Html", "Templates", "InvoiceTemplate.html");
    }

    public string Render()
    {
        // Load the template
        string template = LoadTemplate();

        // Build all the placeholder values
        var placeholders = BuildPlaceholders();

        // Replace all placeholders in the template
        string html = ReplacePlaceholders(template, placeholders);

        return html;
    }

    private string LoadTemplate()
    {
        try
        {
            if (!File.Exists(_templatePath))
            {
                throw new FileNotFoundException($"Template file not found: {_templatePath}");
            }

            return File.ReadAllText(_templatePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error loading invoice template: {ex.Message}", ex);
        }
    }

    private Dictionary<string, string> BuildPlaceholders()
    {
        var placeholders = new Dictionary<string, string>
        {
            // Company Information
            { "{{COMPANY_NAME}}", HtmlEncode(_settings.CompanyName) },
            { "{{COMPANY_ADDRESS}}", BuildCompanyAddress() },
            { "{{COMPANY_PHONE}}", BuildCompanyPhone() },
            { "{{COMPANY_GSTIN}}", BuildCompanyGstin() },
            { "{{COMPANY_HSN_CODE}}", BuildCompanyHsnCode() },
            { "{{COMPANY_HSN_CODE_SERVICE}}", BuildCompanyHsnCodeService() },

            // Invoice Information
            { "{{INVOICE_NUMBER}}", HtmlEncode(_invoice.InvoiceNumber) },
            { "{{INVOICE_DATE}}", _invoice.InvoiceDate.ToString("dd-MMM-yyyy") },
            { "{{INVOICE_OUR_ORDER_REF}}", BuildInvoiceOurOrderRef() },
            { "{{INVOICE_YOUR_ORDER_REF}}", BuildInvoiceYourOrderRef() },

            // Customer Information
            { "{{CUSTOMER_NAME}}", HtmlEncode(_invoice.Customer.Name) },
            { "{{CUSTOMER_BILLING_ADDRESS}}", BuildCustomerBillingAddress() },
            { "{{CUSTOMER_EMAIL}}", BuildCustomerEmail() },
            { "{{CUSTOMER_PHONE}}", BuildCustomerPhone() },
            { "{{CUSTOMER_GSTIN}}", BuildCustomerGstin() },
            { "{{CUSTOMER_SHIPPING_ADDRESS}}", BuildCustomerShippingAddress() },

            // Invoice Items
            { "{{INVOICE_ITEMS}}", BuildInvoiceItems() },

            // Summary Information
            { "{{TAX_BREAKDOWN}}", BuildTaxBreakdown() },
            { "{{INVOICE_SUBTOTAL}}", _invoice.Subtotal.ToString("N2") },
            { "{{INVOICE_TAX_AMOUNT}}", _invoice.TaxAmount.ToString("N2") },
            { "{{INVOICE_DISCOUNT}}", BuildInvoiceDiscount() },
            { "{{INVOICE_NET_AMOUNT}}", _invoice.NetAmount.ToString("N2") },

            // Amount and Footer
            { "{{AMOUNT_IN_WORDS}}", HtmlEncode(BillingSuite.Domain.Utility.ConvertNumberToWords(_invoice.NetAmount)) },
            { "{{TERMS_CONDITIONS}}", BuildTermsAndConditions() },
            { "{{GENERATED_DATE}}", DateTime.Now.ToString("dd-MMM-yyyy HH:mm") }
        };

        return placeholders;
    }

    private string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        string result = template;
        foreach (var kvp in placeholders)
        {
            result = result.Replace(kvp.Key, kvp.Value);
        }
        return result;
    }

    #region Placeholder Builders

    private string BuildCompanyAddress()
    {
        if (string.IsNullOrWhiteSpace(_settings.Address))
            return string.Empty;

        return $"<p>{HtmlEncode(_settings.Address)}</p>";
    }

    private string BuildCompanyPhone()
    {
        if (string.IsNullOrWhiteSpace(_settings.Phone))
            return string.Empty;

        return $"<p><strong>Phone:</strong> {HtmlEncode(_settings.Phone)}</p>";
    }

    private string BuildCompanyGstin()
    {
        if (string.IsNullOrWhiteSpace(_settings.Gstin))
            return string.Empty;

        return $"<p><strong>GSTIN:</strong> {HtmlEncode(_settings.Gstin)}</p>";
    }

    private string BuildCompanyHsnCode()
    {
        if (string.IsNullOrWhiteSpace(_settings.HsnCode))
            return string.Empty;

        return $"<p><strong>HSN Code:</strong> {HtmlEncode(_settings.HsnCode)}</p>";
    }

    private string BuildCompanyHsnCodeService()
    {
        if (string.IsNullOrWhiteSpace(_settings.HsnCodeService))
            return string.Empty;

        return $"<p><strong>HSN Code Service:</strong> {HtmlEncode(_settings.HsnCodeService)}</p>";
    }

    private string BuildInvoiceOurOrderRef()
    {
        if (string.IsNullOrWhiteSpace(_invoice.OurOrderReference))
            return string.Empty;

        return $"<p><span class='label'>Our Order Ref:</span> {HtmlEncode(_invoice.OurOrderReference)}</p>";
    }

    private string BuildInvoiceYourOrderRef()
    {
        if (string.IsNullOrWhiteSpace(_invoice.YourOrderReference))
            return string.Empty;

        return $"<p><span class='label'>Your Order Ref:</span> {HtmlEncode(_invoice.YourOrderReference)}</p>";
    }

    private string BuildCustomerBillingAddress()
    {
        if (string.IsNullOrWhiteSpace(_invoice.Customer.BillingAddress))
            return string.Empty;

        var sb = new StringBuilder();
        var lines = _invoice.Customer.BillingAddress.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            sb.Append($"<p>{HtmlEncode(line.Trim())}</p>");
        }
        return sb.ToString();
    }

    private string BuildCustomerEmail()
    {
        if (string.IsNullOrWhiteSpace(_invoice.Customer.Email))
            return string.Empty;

        return $"<p><strong>Email:</strong> {HtmlEncode(_invoice.Customer.Email)}</p>";
    }

    private string BuildCustomerPhone()
    {
        if (string.IsNullOrWhiteSpace(_invoice.Customer.Phone))
            return string.Empty;

        return $"<p><strong>Phone:</strong> {HtmlEncode(_invoice.Customer.Phone)}</p>";
    }

    private string BuildCustomerGstin()
    {
        if (string.IsNullOrWhiteSpace(_invoice.Customer.Gstin))
            return string.Empty;

        return $"<p><strong>GSTIN:</strong> {HtmlEncode(_invoice.Customer.Gstin)}</p>";
    }

    private string BuildCustomerShippingAddress()
    {
        if (!string.IsNullOrWhiteSpace(_invoice.Customer.ShippingAddress) &&
            _invoice.Customer.ShippingAddress != _invoice.Customer.BillingAddress)
        {
            var sb = new StringBuilder();
            sb.Append($"<p><strong>{HtmlEncode(_invoice.Customer.Name)}</strong></p>");

            var lines = _invoice.Customer.ShippingAddress.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                sb.Append($"<p>{HtmlEncode(line.Trim())}</p>");
            }

            return sb.ToString();
        }

        return "<p class='text-muted'>Same as Billing Address</p>";
    }

    private string BuildInvoiceItems()
    {
        var sb = new StringBuilder();
        var serialNo = 1;

        foreach (var item in _invoice.Items)
        {
            var itemTotal = item.LineTotal + item.TaxAmount;
            var taxType = item.TaxSettings?.TaxType ?? "";
            var taxPercent = item.TaxSettings?.TaxPercent ?? 0;
            var taxDisplay = !string.IsNullOrEmpty(taxType) ? $"{taxType} ({taxPercent}%)" : "No Tax";

            sb.Append("<tr>");
            sb.Append($"<td class='text-center'>{serialNo}</td>");
            sb.Append($"<td>{HtmlEncode(item.Description)}</td>");
            sb.Append($"<td class='text-center'>{HtmlEncode(item.HsnCode ?? "N/A")}</td>");
            sb.Append($"<td class='text-center'>{item.Quantity.ToString("0.##")}</td>");
            sb.Append($"<td class='text-right'>{item.UnitPrice.ToString("N2")}</td>");
            sb.Append($"<td class='text-center'>{HtmlEncode(taxDisplay)}</td>");
            sb.Append($"<td class='text-right'>{item.LineTotal.ToString("N2")}</td>");
            sb.Append($"<td class='text-right'>{item.TaxAmount.ToString("N2")}</td>");
            sb.Append($"<td class='text-right'><strong>{itemTotal.ToString("N2")}</strong></td>");
            sb.Append("</tr>");

            serialNo++;
        }

        return sb.ToString();
    }

    private string BuildTaxBreakdown()
    {
        var taxBreakdown = _invoice.Items
            .Where(item => item.TaxSettings != null && item.TaxAmount > 0)
            .GroupBy(item => new { item.TaxSettings!.TaxType, item.TaxSettings.TaxPercent })
            .Select(g => new { g.Key.TaxType, g.Key.TaxPercent, Amount = g.Sum(x => x.TaxAmount) })
            .ToList();

        if (!taxBreakdown.Any())
            return string.Empty;

        var sb = new StringBuilder();
        sb.Append(@"
            <div class='tax-breakdown'>
                <div class='summary-box'>
                    <h4>Tax Breakdown</h4>");
        foreach (var tax in taxBreakdown)
        {
            sb.Append($@"
                    <div class='summary-row'>
                        <span>{tax.TaxType} ({tax.TaxPercent}%)</span>
                        <span>{tax.Amount.ToString("N2")}</span>
                    </div>");
        }
        sb.Append(@"
                </div>
            </div>");

        return sb.ToString();
    }

    private string BuildInvoiceDiscount()
    {
        if (_invoice.DiscountAmount <= 0)
            return string.Empty;

        return $@"
                    <div class='summary-row' style='color: #dc3545;'>
                        <span>Discount:</span>
                        <span>-{_invoice.DiscountAmount.ToString("N2")}</span>
                    </div>";
    }

    private string BuildTermsAndConditions()
    {
        string termsHtml;

        if (!string.IsNullOrWhiteSpace(_settings.TermsAndConditions))
        {
            var sb = new StringBuilder();
            sb.Append(@"
        <div class='terms-conditions'>
            <h4>Terms & Conditions</h4>");

            var termsLines = _settings.TermsAndConditions
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in termsLines.Take(3))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    sb.Append($"<p>{HtmlEncode(line.Trim())}</p>");
                }
            }

            sb.Append(@"
        </div>");

            termsHtml = sb.ToString();
        }
        else
        {
            termsHtml = @"
        <div class='terms-conditions'>
            <h4>Terms & Conditions</h4>
            <p>1. Payment is due within 30 days of invoice date.</p>
            <p>2. Late payments may incur additional charges.</p>
        </div>";
        }

        return termsHtml;
    }

    #endregion

    private string HtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return System.Web.HttpUtility.HtmlEncode(text);
    }
}
