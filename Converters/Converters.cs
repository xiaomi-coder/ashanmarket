using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SupermarketPOS.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool bVal = value is bool b ? b : value != null && !value.Equals(0);
        if (Invert) bVal = !bVal;
        return bVal ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

[ValueConversion(typeof(int), typeof(Visibility))]
public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int i && i > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusColorConverter : IValueConverter
{
    public static readonly StatusColorConverter Instance = new();
    private static readonly SolidColorBrush RedBrush    = new(Color.FromRgb(231, 76, 60));
    private static readonly SolidColorBrush GreenBrush  = new(Color.FromRgb(39, 174, 96));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? RedBrush : GreenBrush;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToStringConverter : IValueConverter
{
    public string TrueValue  { get; set; } = "Ha";
    public string FalseValue { get; set; } = "Yo'q";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string param && param.Contains('|'))
        {
            var parts = param.Split('|');
            return value is bool b && b ? parts[0] : parts[1];
        }
        return value is bool bv && bv ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        if (parameter is string param && param == "Inverse")
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        
        return !isNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(s);
                return new SolidColorBrush(color);
            }
            catch
            {
                // Return default color if invalid
            }
        }
        return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Default blue
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color.ToString();
        }
        return "#2196F3";
    }
}

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isTrue && isTrue)
        {
            return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
        }
        return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isTrue && isTrue) return "Faol";
        return "Bloklangan";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
