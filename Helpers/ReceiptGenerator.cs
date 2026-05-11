using SupermarketPOS.Models;
using System.IO;
using System.Text;

namespace SupermarketPOS.Helpers;

public static class ReceiptGenerator
{
    private const int Width = 42;

    public static string GenerateText(Sale sale, string? storeName = null)
    {
        var sb = new StringBuilder();

        // Read store name if not provided
        var receiptHeader = storeName ?? Services.SettingsManager.Load().StoreName.Replace("🛒", "").Trim().ToUpper();

        // Header
        sb.AppendLine(Center(receiptHeader));
        sb.AppendLine(Center("Bizning do'konimizga xush kelibsiz!"));
        sb.AppendLine(Repeat('-', Width));
        sb.AppendLine($"Chek №:  {sale.SaleNumber}");
        sb.AppendLine($"Sana:    {sale.CreatedAt:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Kassir:  {sale.CashierName}");
        sb.AppendLine($"To'lov:  {sale.PaymentMethod}");
        sb.AppendLine(Repeat('-', Width));

        // Column headers
        sb.AppendLine($"{"Mahsulot",-22} {"Don",4} {"Narx",7} {"Jami",7}");
        sb.AppendLine(Repeat('-', Width));

        // Items
        foreach (var item in sale.Items)
        {
            var name = item.ProductName.Length > 22
                ? item.ProductName[..22]
                : item.ProductName;
            sb.AppendLine($"{name,-22} {item.Quantity,4} {item.UnitPrice,7:N0} {item.Total,7:N0}");
        }

        sb.AppendLine(Repeat('=', Width));

        // Totals
        if (sale.Discount > 0)
        {
            sb.AppendLine($"{"Jami:",-(Width - 10)} {sale.SubTotal,10:N0}");
            sb.AppendLine($"{"Chegirma:",-(Width - 10)} -{sale.Discount,9:N0}");
        }
        if (sale.Tax > 0)
        {
            sb.AppendLine($"{"QQS:",-(Width - 10)} {sale.Tax,10:N0}");
        }
        sb.AppendLine($"{"JAMI TO'LOV:",-(Width - 10)} {sale.Total,10:N0}");
        sb.AppendLine($"{"Qabul qilindi:",-(Width - 10)} {sale.AmountPaid,10:N0}");
        sb.AppendLine($"{"Qaytim:",-(Width - 10)} {sale.Change,10:N0}");
        sb.AppendLine(Repeat('=', Width));

        // Footer
        sb.AppendLine(Center("Xaridingiz uchun rahmat!"));
        sb.AppendLine(Center("Yana kutib qolamiz!"));
        sb.AppendLine(Repeat('-', Width));
        sb.AppendLine(Center(DateTime.Now.ToString("yyyy")));

        return sb.ToString();
    }

    public static void PrintToFile(Sale sale, string path)
    {
        var text = GenerateText(sale);
        File.WriteAllText(path, text, Encoding.UTF8);
    }

    private static string Center(string text)
    {
        if (text.Length >= Width) return text;
        var padding = (Width - text.Length) / 2;
        return text.PadLeft(padding + text.Length).PadRight(Width);
    }

    private static string Repeat(char c, int count) => new(c, count);
}
