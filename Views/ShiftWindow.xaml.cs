using System.Windows;

namespace SupermarketPOS.Views;

public partial class ShiftWindow : Window
{
    public ShiftWindow()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
