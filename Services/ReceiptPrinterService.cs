using SupermarketPOS.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SupermarketPOS.Services;

public interface IReceiptPrinterService
{
    void PrintReceipt(Sale sale);
}

public class ReceiptPrinterService : IReceiptPrinterService
{
    public void PrintReceipt(Sale sale)
    {
        // For thermal printer, usually width is about 280-300 pixels (80mm printer)
        double width = 300;

        var doc = new FlowDocument
        {
            PagePadding = new Thickness(10),
            ColumnWidth = width,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12
        };

        // Header
        var header = new Paragraph(new Run("SUPERMARKET POS"))
        {
            TextAlignment = TextAlignment.Center,
            FontWeight = FontWeights.Bold,
            FontSize = 18,
            Margin = new Thickness(0, 0, 0, 10)
        };
        doc.Blocks.Add(header);

        // Info
        var info = new Paragraph();
        info.Inlines.Add(new Run($"Sana: {sale.CreatedAt:dd.MM.yyyy HH:mm}\n"));
        info.Inlines.Add(new Run($"Chek: {sale.SaleNumber}\n"));
        info.Inlines.Add(new Run($"Kassir: {sale.CashierName ?? "Kassir"}\n"));
        info.Inlines.Add(new Run(new string('-', 38)));
        doc.Blocks.Add(info);

        // Items
        var itemsTable = new Table();
        itemsTable.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) }); // Name
        itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) }); // Qty
        itemsTable.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) }); // Total

        var rowGroup = new TableRowGroup();
        foreach (var item in sale.Items)
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run(item.ProductName))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(item.Quantity.ToString("N0")))) { TextAlignment = TextAlignment.Center });
            row.Cells.Add(new TableCell(new Paragraph(new Run(item.Total.ToString("N0")))) { TextAlignment = TextAlignment.Right });
            rowGroup.Rows.Add(row);
        }
        itemsTable.RowGroups.Add(rowGroup);
        doc.Blocks.Add(itemsTable);

        // Separator
        doc.Blocks.Add(new Paragraph(new Run(new string('-', 38))));

        // Totals
        var totals = new Paragraph();
        totals.TextAlignment = TextAlignment.Right;
        totals.Inlines.Add(new Run($"Chegirma: {sale.Discount:N0}\n"));
        totals.Inlines.Add(new Run($"Jami: {sale.Total:N0}\n") { FontWeight = FontWeights.Bold, FontSize = 16 });
        totals.Inlines.Add(new Run($"To'lov turi: {sale.PaymentMethod}\n"));
        doc.Blocks.Add(totals);

        // Footer
        var footer = new Paragraph(new Run("XARIDINGIZ UCHUN RAHMAT!"))
        {
            TextAlignment = TextAlignment.Center,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 20, 0, 0)
        };
        doc.Blocks.Add(footer);

        // Print dialog (you can skip the dialog and print directly to default printer in production)
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
        {
            IDocumentPaginatorSource idpSource = doc;
            dialog.PrintDocument(idpSource.DocumentPaginator, $"Receipt_{sale.SaleNumber}");
        }
    }
}
