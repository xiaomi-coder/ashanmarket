using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SupermarketPOS.Models;
using SupermarketPOS.ViewModels;

namespace SupermarketPOS.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        BarcodeBox.Focus();

        // Register F5 shortcut
        Window.GetWindow(this)?.CommandBindings.Add(
            new CommandBinding(
                new RoutedCommand("CompleteSale", typeof(SalesView),
                    new InputGestureCollection { new KeyGesture(Key.F5) }),
                (s, _) =>
                {
                    if (DataContext is SalesViewModel vm && vm.CompleteSaleCommand.CanExecute(null))
                        vm.CompleteSaleCommand.Execute(null);
                }));
    }

    private void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is SalesViewModel vm)
            {
                // If it looks like a search query, search; otherwise scan barcode
                if (vm.BarcodeInput.Length > 3 && !vm.BarcodeInput.All(char.IsDigit))
                    vm.SearchQuery = vm.BarcodeInput;
                else if (vm.ScanBarcodeCommand.CanExecute(null))
                    vm.ScanBarcodeCommand.Execute(null);
            }
        }
    }

    private void SearchItem_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListViewItem item && item.Content is Product product)
        {
            if (DataContext is SalesViewModel vm)
                vm.AddToCart(product);
        }
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CartItem item &&
            DataContext is SalesViewModel vm)
        {
            vm.RemoveItemCommand.Execute(item);
        }
    }

    private void IncreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CartItem item &&
            DataContext is SalesViewModel vm)
        {
            item.Quantity++;
            vm.RecalculateTotals();
        }
    }

    private void DecreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CartItem item &&
            DataContext is SalesViewModel vm)
        {
            if (item.Quantity > 1) item.Quantity--;
            vm.RecalculateTotals();
        }
    }

    private void QuickAmount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && decimal.TryParse(btn.Tag?.ToString(), out var amount))
        {
            if (DataContext is SalesViewModel vm)
                vm.AmountPaid = amount;
        }
    }
}
