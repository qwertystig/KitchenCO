using System.Collections.Generic;
using System.Threading.Tasks;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services.Export;

/// <summary>
/// The ONE definition of what an invoice says. The customer's invoice
/// screen (share text + PDF download) and the admin's "invoices for the
/// day" PDF both come from here, so what the client sees on their phone
/// and what the kitchen prints are the same document line for line.
/// </summary>
public sealed class InvoiceDocument
{
    public const string SupplierName = "Your Kitchen Co. (Pty) Ltd";
    public const string SupplierVat = "VAT Reg No: 4910298412";
    public const string SupplierAddress = "Corporate Culinary Hub, Sandton, 2196";
    public const string VatNote = "VAT: Not applicable - zero-rated corporate catering.";

    public Order Order { get; }
    public string CompanyName { get; }
    public string LocationName { get; }
    public string LocationAddress { get; }

    public InvoiceDocument(Order order, string companyName, string locationName, string locationAddress)
    {
        Order = order;
        CompanyName = companyName;
        LocationName = locationName;
        LocationAddress = locationAddress;
    }

    /// <summary>Backs the subtotal out of the stored components (Order only stores the final total).</summary>
    public decimal MealSubtotal => Order.TotalAmount - Order.DeliveryFee + Order.SubsidyAmount + Order.DiscountAmount;

    public static async Task<InvoiceDocument> LoadAsync(Order order, ICompanyDirectoryService companyDirectory)
    {
        var company = !string.IsNullOrWhiteSpace(order.CompanyId) ? await companyDirectory.GetCompanyAsync(order.CompanyId) : null;
        var location = !string.IsNullOrWhiteSpace(order.LocationId) ? await companyDirectory.GetLocationAsync(order.LocationId) : null;

        return new InvoiceDocument(
            order,
            company?.Name ?? "Individual Customer",
            location?.Name ?? "-",
            location?.Address ?? location?.Name ?? "-");
    }

    /// <summary>Plain-text rendering — used for the share-as-text option and as the body of the PDF.</summary>
    public string ToText()
    {
        var (dish, qty) = Order.ParseItemNameAndQuantity();
        var lines = new List<string>
        {
            SupplierName.ToUpperInvariant(),
            "INVOICE",
            $"Invoice No:     {Order.TaxInvoiceNumber}",
            $"Order No:       #{Order.OrderNumber}",
            $"Issued:         {Order.OrderDate:dd MMM yyyy HH:mm}",
            SupplierVat,
            SupplierAddress,
            "",
            "CUSTOMER & DROP-OFF",
            $"Name:           {Order.CustomerName}",
            $"Company:        {CompanyName}",
            $"Location:       {LocationName}",
            $"Address:        {LocationAddress}",
        };
        if (!string.IsNullOrWhiteSpace(Order.DeliveryFloor))
            lines.Add($"Floor / Desk:   {Order.DeliveryFloor}");
        lines.Add($"Delivery Date:  {Order.DeliveryDate:dddd, dd MMM yyyy}");
        lines.Add($"Status:         {Order.Status}");
        lines.Add("");
        lines.Add("LINE ITEMS");
        lines.Add($"  {qty} x {dish}".PadRight(44) + $"R {MealSubtotal,10:F2}");
        if (!string.IsNullOrWhiteSpace(Order.SummaryText))
            lines.Add($"  Note: {Order.SummaryText}");
        if (Order.HasAllergyNotes)
            lines.Add($"  ALLERGY: {Order.AllergyNotes}");
        lines.Add("");
        lines.Add("Meal Subtotal:".PadRight(44) + $"R {MealSubtotal,10:F2}");
        if (Order.SubsidyAmount > 0)
            lines.Add("Company Subsidy:".PadRight(44) + $"-R {Order.SubsidyAmount,9:F2}");
        if (Order.DiscountAmount > 0)
            lines.Add("Corporate Discount:".PadRight(44) + $"-R {Order.DiscountAmount,9:F2}");
        lines.Add("Delivery Fee:".PadRight(44) + $"R {Order.DeliveryFee,10:F2}");
        lines.Add(new string('-', 56));
        lines.Add("TOTAL PAID (ZAR):".PadRight(44) + $"R {Order.TotalAmount,10:F2}");
        lines.Add("");
        lines.Add(VatNote);
        return string.Join("\n", lines);
    }

    /// <summary>Appends this invoice to a PDF (one invoice per page when batching — see <paramref name="pageBreakBefore"/>).</summary>
    public void WriteTo(SimplePdfWriter pdf, bool pageBreakBefore = false)
    {
        if (pageBreakBefore) pdf.PageBreak();

        pdf.Heading(SupplierName, 15, 2);
        pdf.Text(SupplierVat, 9);
        pdf.Text(SupplierAddress, 9, 10);

        pdf.Heading("INVOICE", 20, 2);
        pdf.Text($"Invoice No: {Order.TaxInvoiceNumber}    Order No: #{Order.OrderNumber}    Issued: {Order.OrderDate:dd MMM yyyy HH:mm}", 10, 12);

        pdf.SubHeading("CUSTOMER & DROP-OFF", 11, 2);
        pdf.Text($"Name: {Order.CustomerName}", 10);
        pdf.Text($"Company: {CompanyName}", 10);
        pdf.Text($"Location: {LocationName}", 10);
        pdf.Text($"Address: {LocationAddress}", 10);
        if (!string.IsNullOrWhiteSpace(Order.DeliveryFloor))
            pdf.Text($"Floor / Desk: {Order.DeliveryFloor}", 10);
        pdf.Text($"Delivery Date: {Order.DeliveryDate:dddd, dd MMM yyyy}    Status: {Order.Status}", 10, 12);

        var (dish, qty) = Order.ParseItemNameAndQuantity();
        pdf.SubHeading("LINE ITEMS", 11, 2);
        pdf.Mono($"  {qty} x {dish}".PadRight(52) + $"R {MealSubtotal,10:F2}", 10);
        if (!string.IsNullOrWhiteSpace(Order.SummaryText)) pdf.Mono($"  Note: {Order.SummaryText}", 9);
        if (Order.HasAllergyNotes) pdf.Mono($"  ALLERGY: {Order.AllergyNotes}", 9);
        pdf.Blank();

        pdf.Mono("Meal Subtotal:".PadRight(52) + $"R {MealSubtotal,10:F2}", 10);
        if (Order.SubsidyAmount > 0) pdf.Mono("Company Subsidy:".PadRight(52) + $"-R {Order.SubsidyAmount,9:F2}", 10);
        if (Order.DiscountAmount > 0) pdf.Mono("Corporate Discount:".PadRight(52) + $"-R {Order.DiscountAmount,9:F2}", 10);
        pdf.Mono("Delivery Fee:".PadRight(52) + $"R {Order.DeliveryFee,10:F2}", 10);
        pdf.Rule(64);
        pdf.Add("TOTAL PAID (ZAR):".PadRight(52) + $"R {Order.TotalAmount,10:F2}", SimplePdfWriter.Font.Mono, 11, 10);
        pdf.Text(VatNote, 9);
    }
}
