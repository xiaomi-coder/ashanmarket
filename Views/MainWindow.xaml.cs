using System.Windows;
using System.Windows.Input;
using SupermarketPOS.ViewModels;

namespace SupermarketPOS.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        switch (e.Key)
        {
            case Key.F1:
                vm.NavigateSalesCommand.Execute(null);
                break;
            case Key.F2 when vm.IsAdmin:
                vm.NavigateProductsCommand.Execute(null);
                break;
            case Key.F3 when vm.IsAdmin:
                vm.NavigateReportsCommand.Execute(null);
                break;
        }
    }
}
