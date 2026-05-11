using System.Linq;
using System.Windows.Controls;

namespace SupermarketPOS.Views;

public partial class ExpensesView : UserControl
{
    public ExpensesView()
    {
        InitializeComponent();
    }

    private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            int caretFromRight = tb.Text.Length - tb.CaretIndex;

            string cleanText = new string(tb.Text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(cleanText))
            {
                if (tb.Text != "") tb.Text = "";
                return;
            }

            if (decimal.TryParse(cleanText, out decimal amount))
            {
                var newText = amount.ToString("N0");
                if (tb.Text != newText)
                {
                    tb.Text = newText;
                    tb.CaretIndex = System.Math.Max(0, tb.Text.Length - caretFromRight);
                }
            }
        }
    }
}
