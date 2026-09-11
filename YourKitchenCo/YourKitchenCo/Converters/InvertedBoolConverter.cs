using System.Globalization;

namespace YourKitchenCo.Converters;

public class InvertedBoolConverter : IValueConverter
{
    // Converts bool to inverted bool (e.g., true becomes false)
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}