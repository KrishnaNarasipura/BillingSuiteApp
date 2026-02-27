using BillingSuite.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BillingSuite.Infrastructure.Services.Pdf;

public class InvoicePdf
{
    private readonly CompanySettings _settings;
    private readonly Invoice _invoice;

    public InvoicePdf(CompanySettings settings, Invoice invoice)
    {
        _settings = settings;
        _invoice = invoice;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render()
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);

                page.Header().Row(row =>
                {
                    // Company Information
                    row.RelativeItem().Column(stack =>
                    {
                        stack.Item().Text(_settings.CompanyName).FontSize(22).SemiBold().FontColor(Colors.Blue.Medium);
                        if (!string.IsNullOrWhiteSpace(_settings.Address))
                            stack.Item().Text(_settings.Address).FontSize(10);
                        if (!string.IsNullOrWhiteSpace(_settings.Phone))
                            stack.Item().Text($"Phone: {_settings.Phone}").FontSize(10);
                        if (!string.IsNullOrWhiteSpace(_settings.Gstin))
                            stack.Item().Text($"GSTIN: {_settings.Gstin}").FontSize(10);
                    });

                    // Invoice Information
                    row.RelativeItem().Column(stack =>
                    {
                        stack.Item().AlignRight().Text("INVOICE").FontSize(18).SemiBold().FontColor(Colors.Green.Medium);
                        stack.Item().AlignRight().Text($"Invoice #: {_invoice.InvoiceNumber}").FontSize(12).SemiBold();
                        stack.Item().AlignRight().Text($"Date: {_invoice.InvoiceDate:dd-MMM-yyyy}").FontSize(11);
                    });
                });

                page.Content().Column(stack =>
                {
                    // Customer Information Section
                    stack.Item().PaddingTop(20).Row(row =>
                    {
                        // Billing Address
                        row.RelativeItem().Column(billCol =>
                        {
                            billCol.Item().Text("Bill To:").FontSize(12).SemiBold().FontColor(Colors.Grey.Darken3);
                            billCol.Item().PaddingTop(5).Column(billStack =>
                            {
                                billStack.Item().Text(_invoice.Customer.Name).FontSize(11).SemiBold();
                                if (!string.IsNullOrWhiteSpace(_invoice.Customer.BillingAddress))
                                    billStack.Item().Text(_invoice.Customer.BillingAddress).FontSize(10);
                                if (!string.IsNullOrWhiteSpace(_invoice.Customer.Email))
                                    billStack.Item().Text($"Email: {_invoice.Customer.Email}").FontSize(10);
                                if (!string.IsNullOrWhiteSpace(_invoice.Customer.Phone))
                                    billStack.Item().Text($"Phone: {_invoice.Customer.Phone}").FontSize(10);
                                if (!string.IsNullOrWhiteSpace(_invoice.Customer.Gstin))
                                    billStack.Item().Text($"GSTIN: {_invoice.Customer.Gstin}").FontSize(10);
                            });
                        });

                        // Shipping Address (if different from billing)
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.ShippingAddress) &&
                            _invoice.Customer.ShippingAddress != _invoice.Customer.BillingAddress)
                        {
                            row.RelativeItem().Column(shipCol =>
                            {
                                shipCol.Item().Text("Ship To:").FontSize(12).SemiBold().FontColor(Colors.Grey.Darken3);
                                shipCol.Item().PaddingTop(5).Column(shipStack =>
                                {
                                    shipStack.Item().Text(_invoice.Customer.Name).FontSize(11).SemiBold();
                                    shipStack.Item().Text(_invoice.Customer.ShippingAddress).FontSize(10);
                                });
                            });
                        }
                        else
                        {
                            // Empty column to maintain layout balance
                            row.RelativeItem().Text("");
                        }
                    });

                    // Invoice Items Table
                    stack.Item().PaddingTop(30).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4); // Description
                            cols.RelativeColumn(1); // Qty
                            cols.RelativeColumn(2); // Unit Price
                            cols.RelativeColumn(1.5f); // Tax Type
                            cols.RelativeColumn(1.5f); // Line Total
                            cols.RelativeColumn(1.5f); // Tax Amount
                            cols.RelativeColumn(2); // Item Total
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(CellHeader).Text("Description");
                            h.Cell().Element(CellHeader).AlignCenter().Text("Qty");
                            h.Cell().Element(CellHeader).AlignRight().Text("Unit Price");
                            h.Cell().Element(CellHeader).AlignCenter().Text("Tax Type");
                            h.Cell().Element(CellHeader).AlignRight().Text("Line Total");
                            h.Cell().Element(CellHeader).AlignRight().Text("Tax Amount");
                            h.Cell().Element(CellHeader).AlignRight().Text("Item Total");
                        });

                        foreach (var item in _invoice.Items)
                        {
                            var itemTotal = item.LineTotal + item.TaxAmount;
                            var taxType = item.TaxSettings?.TaxType ?? "";
                            var taxPercent = item.TaxSettings?.TaxPercent ?? 0;
                            var taxDisplay = !string.IsNullOrEmpty(taxType) ? $"{taxType} ({taxPercent}%)" : "No Tax";

                            table.Cell().Element(Cell).Text(item.Description);
                            table.Cell().Element(Cell).AlignCenter().Text(item.Quantity.ToString("0.##"));
                            table.Cell().Element(Cell).AlignRight().Text(item.UnitPrice.ToString(""));
                            table.Cell().Element(Cell).AlignCenter().Text(taxDisplay).FontSize(8);
                            table.Cell().Element(Cell).AlignRight().Text(item.LineTotal.ToString(""));
                            table.Cell().Element(Cell).AlignRight().Text(item.TaxAmount.ToString(""));
                            table.Cell().Element(Cell).AlignRight().Text(itemTotal.ToString("")).SemiBold();
                        }
                    });

                    // Summary Section
                    stack.Item().PaddingTop(20).Row(row =>
                    {
                        // Left side - Tax Breakdown (if applicable)
                        row.RelativeItem().Column(leftCol =>
                        {
                            var taxBreakdown = _invoice.Items
                                .Where(item => item.TaxSettings != null && item.TaxAmount > 0)
                                .GroupBy(item => new { item.TaxSettings!.TaxType, item.TaxSettings.TaxPercent })
                                .Select(g => new { g.Key.TaxType, g.Key.TaxPercent, Amount = g.Sum(x => x.TaxAmount) })
                                .ToList();

                            if (taxBreakdown.Any())
                            {
                                leftCol.Item().Text("Tax Breakdown:").FontSize(10).SemiBold();
                                foreach (var tax in taxBreakdown)
                                {
                                    leftCol.Item().Row(taxRow =>
                                    {
                                        taxRow.RelativeItem().Text($"{tax.TaxType} ({tax.TaxPercent}%):").FontSize(9);
                                        taxRow.RelativeItem().AlignRight().Text(tax.Amount.ToString("")).FontSize(9);
                                    });
                                }
                            }
                        });

                        // Right side - Summary totals
                        row.RelativeItem().Column(rightCol =>
                        {
                            rightCol.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(summaryCol =>
                            {
                                summaryCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Subtotal:").FontSize(11);
                                    r.RelativeItem().AlignRight().Text(_invoice.Subtotal.ToString("")).FontSize(11);
                                });

                                summaryCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Total Tax:").FontSize(11);
                                    r.RelativeItem().AlignRight().Text(_invoice.TaxAmount.ToString("")).FontSize(11);
                                });

                                if (_invoice.DiscountAmount > 0)
                                {
                                    summaryCol.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Discount:").FontSize(11).FontColor(Colors.Red.Medium);
                                        r.RelativeItem().AlignRight().Text($"-{_invoice.DiscountAmount}").FontSize(11).FontColor(Colors.Red.Medium);
                                    });
                                }

                                summaryCol.Item().BorderTop(1).BorderColor(Colors.Grey.Medium).PaddingTop(10).Row(r =>
                                {
                                    r.RelativeItem().Text("Net Amount:").FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                                    r.RelativeItem().AlignRight().Text(_invoice.NetAmount.ToString("")).FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                                });
                            });
                        });
                    });

                    // Amount in Words
                    stack.Item().PaddingTop(20).Row(r =>
                    {
                        r.RelativeItem().Text("Amount in Words: ").FontSize(10).SemiBold();
                        r.RelativeItem(3).Text(ConvertAmountToWords(_invoice.NetAmount)).FontSize(10).Italic();
                    });

                    // Terms and Conditions (Optional)
                    if (!string.IsNullOrWhiteSpace(_settings.TermsAndConditions))
                    {
                        stack.Item().PaddingTop(20).Column(termsCol =>
                        {
                            termsCol.Item().Text("Terms & Conditions:").FontSize(10).SemiBold();

                            // Split terms and conditions by line breaks and display each line
                            var termsLines = _settings.TermsAndConditions
                                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (var line in termsLines)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    termsCol.Item().Text(line.Trim()).FontSize(9);
                                }
                            }
                        });
                    }
                    else
                    {
                        // Fallback to default terms if none are set in company settings
                        stack.Item().PaddingTop(20).Column(termsCol =>
                        {
                            termsCol.Item().Text("Terms & Conditions:").FontSize(10).SemiBold();
                            termsCol.Item().Text("1. Payment is due within 30 days of invoice date.").FontSize(9);
                            termsCol.Item().Text("2. Late payments may incur additional charges.").FontSize(9);
                            termsCol.Item().Text("3. All disputes must be reported within 7 days.").FontSize(9);
                        });
                    }
                });

                page.Footer().AlignCenter().Column(footerCol =>
                {
                    footerCol.Item().Text("Thank you for your business!").FontSize(10).Italic();
                    footerCol.Item().Text("Generated by BillingSuite").FontSize(8).FontColor(Colors.Grey.Medium);
                    footerCol.Item().Text($"Generated on: {DateTime.Now:dd-MMM-yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();

        static IContainer CellHeader(IContainer c) =>
            c.Background(Colors.Grey.Lighten3)
             .BorderBottom(1)
             .BorderColor(Colors.Grey.Medium)
             .Padding(8)
             .DefaultTextStyle(TextStyle.Default.SemiBold().FontSize(10));

        static IContainer Cell(IContainer c) =>
            c.BorderBottom(1)
             .BorderColor(Colors.Grey.Lighten2)
             .Padding(6)
             .DefaultTextStyle(TextStyle.Default.FontSize(9));
    }

    private string ConvertAmountToWords(decimal amount)
    {
        // Use the existing utility method if available
        try
        {
            return BillingSuite.Domain.Utility.ConvertNumberToWords(amount);
        }
        catch
        {
            return $"{amount:C} (Amount conversion not available)";
        }
    }
}