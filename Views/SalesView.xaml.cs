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

    private void Quantity_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is SalesViewModel vm)
        {
            vm.RecalculateTotals();
        }
    }

    private void QuickAmount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && decimal.TryParse(btn.Tag?.ToString(), out var amount))
        {
            if (DataContext is SalesViewModel vm)
                vm.AmountPaid = amount.ToString();
        }
    }

    private void Numpad_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null && DataContext is SalesViewModel vm)
        {
            string tag = btn.Tag.ToString() ?? "";
            string current = vm.AmountPaid;
            if (current == "0") current = "";

            if (tag == "BACK")
            {
                if (current.Length > 0)
                {
                    current = current.Substring(0, current.Length - 1);
                }
            }
            else
            {
                current += tag;
            }

            if (string.IsNullOrEmpty(current)) current = "0";

            if (decimal.TryParse(current, out _))
            {
                vm.AmountPaid = current;
            }
        }
    }

    private Point _scrollMousePoint;
    private double _hOff = 1;
    private bool _isDragging = false;
    private Point _previousMousePoint;
    private DateTime _lastMouseMoveTime;
    private double _velocity = 0;
    private System.Windows.Threading.DispatcherTimer? _inertiaTimer;

    private void Categories_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            var sv = GetScrollViewer(listBox);
            if (sv != null)
            {
                sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }


    private ScrollViewer? GetScrollViewer(DependencyObject depObj)
    {
        if (depObj is ScrollViewer sv) return sv;
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
            var result = GetScrollViewer(child);
            if (result != null) return result;
        }
        return null;
    }

    private T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindParent<T>(parentObject);
    }
}
